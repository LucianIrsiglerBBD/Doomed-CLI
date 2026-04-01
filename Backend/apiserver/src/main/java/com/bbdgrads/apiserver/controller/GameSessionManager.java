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
import com.bbdgrads.apiserver.services.UserService;

import lombok.RequiredArgsConstructor;

@RestController
@RequestMapping("api/game")
@RequiredArgsConstructor

class GameSessionManager {

    private final Map<String, List<GameSessionRequest>> gameSession = new ConcurrentHashMap<>();
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

        for (int index = 0; index < session.size(); index++) {
            var existingRequest = session.get(index);
            if (existingRequest.getUsername().equals(request.getUsername())) {
                session.set(index, request);
                return ResponseEntity.ok().build();
            }
        }

        session.add(request);
        return ResponseEntity.ok().build();
    }
}
