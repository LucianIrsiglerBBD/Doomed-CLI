package com.bbdgrads.apiserver.controller;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.bbdgrads.apiserver.dto.GameSessionRequest;
import com.bbdgrads.apiserver.dto.HitRequest;
import com.bbdgrads.apiserver.services.UserService;

import lombok.RequiredArgsConstructor;

@RestController
@RequestMapping("api/game")
@RequiredArgsConstructor

class GameSessionManager {

    private record KillRecord(String killer, String victim, String weapon) {}

    private final Map<String, List<GameSessionRequest>> gameSession = new ConcurrentHashMap<>();
    private final Map<String, List<KillRecord>> killRecords = new ConcurrentHashMap<>();
    private final UserService userService;

    @GetMapping("/{userName}")
    public ResponseEntity<List<GameSessionRequest>> getGameSession(@PathVariable String userName) {
        var user = userService.getUserByName(userName);
        var lobbyCode = user.getLobby().getCode();

        var session = gameSession.get(lobbyCode);
        if (session == null) {
            return ResponseEntity.notFound().build();
        }
        return ResponseEntity.ok(session);
    }

    @PostMapping
    public ResponseEntity<Void> setGameSession(@RequestBody GameSessionRequest request) {
        var user = userService.getUserByName(request.getUsername());
        var lobbyCode = user.getLobby().getCode();

        var session = gameSession.computeIfAbsent(lobbyCode, ignored -> new ArrayList<>());

        synchronized (session) {
            for (int index = 0; index < session.size(); index++) {
                var existingRequest = session.get(index);
                if (existingRequest.getUsername().equals(request.getUsername())) {
                    // Preserve server-authoritative health — only accept position updates here.
                    // Health is modified exclusively by the /hit endpoint.
                    var preserved = new GameSessionRequest(
                            request.getUsername(),
                            request.getX(),
                            request.getY(),
                            existingRequest.getHealth());
                    session.set(index, preserved);
                    return ResponseEntity.ok().build();
                }
            }
            session.add(request);
        }
        return ResponseEntity.ok().build();
    }

    @PostMapping("/hit")
    public ResponseEntity<Void> processHit(@RequestBody HitRequest request) {
        var targetUser = userService.getUserByName(request.getTarget());
        if (targetUser == null || targetUser.getLobby() == null) {
            return ResponseEntity.notFound().build();
        }
        var lobbyCode = targetUser.getLobby().getCode();

        var session = gameSession.get(lobbyCode);
        if (session == null) {
            return ResponseEntity.notFound().build();
        }

        synchronized (session) {
            for (int i = 0; i < session.size(); i++) {
                var entry = session.get(i);
                if (entry.getUsername().equals(request.getTarget())) {
                    int prevHealth = entry.getHealth();
                    int newHealth = Math.max(0, prevHealth - request.getDamage());

                    session.set(i, new GameSessionRequest(
                            entry.getUsername(), entry.getX(), entry.getY(), newHealth));

                    if (prevHealth > 0 && newHealth == 0) {
                        killRecords
                            .computeIfAbsent(lobbyCode, ignored -> new ArrayList<>())
                            .add(new KillRecord(request.getShooter(), request.getTarget(), request.getWeaponName()));
                    }
                    return ResponseEntity.ok().build();
                }
            }
        }
        return ResponseEntity.notFound().build();
    }

    @GetMapping("/kills/{lobbyCode}")
    public ResponseEntity<List<KillRecord>> getKills(@PathVariable String lobbyCode) {
        var kills = killRecords.getOrDefault(lobbyCode, List.of());
        return ResponseEntity.ok(kills);
    }
}
