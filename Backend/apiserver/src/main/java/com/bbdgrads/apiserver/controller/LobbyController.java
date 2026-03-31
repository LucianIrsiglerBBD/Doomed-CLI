package com.bbdgrads.apiserver.controller;

import java.util.List;
import java.util.Map;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import com.bbdgrads.apiserver.model.Lobby;
import com.bbdgrads.apiserver.model.UsersGameInformation;
import com.bbdgrads.apiserver.services.LobbyService;
import lombok.RequiredArgsConstructor;

@RestController
@RequestMapping("api/lobbies")
@RequiredArgsConstructor
public class LobbyController {

    private final LobbyService lobbyService;

    @PostMapping
    public ResponseEntity<Lobby> createLobby(@RequestBody Map<String, Object> createReq) {
        int hostUserId = Integer.valueOf(createReq.get("hostUserId").toString());
        int mapId = Integer.valueOf(createReq.get("mapId").toString());

        int durationInMinutes = (int) createReq.getOrDefault("durationInMinutes", 30);

        Lobby lobby = lobbyService.createLobby(hostUserId, mapId, durationInMinutes);
        return new ResponseEntity<>(lobby, HttpStatus.CREATED);
    }

    @PostMapping("/{lobbyCode}/join")
    public ResponseEntity<UsersGameInformation> joinLobby(@PathVariable String lobbyCode,
            @RequestBody Map<String, Integer> joinRequest) {
        int userId = joinRequest.get("userId");
        UsersGameInformation usersGameInformation = lobbyService.joinLobby(userId, lobbyCode);
        return ResponseEntity.ok(usersGameInformation);
    }

    @PostMapping("/{lobbyId}/leave")
    public ResponseEntity<Void> leaveLobby(@PathVariable int lobbyId, @RequestBody Map<String, Integer> leaveRequest) {
        int userId = leaveRequest.get("userId");
        lobbyService.leaveLobby(userId, lobbyId);
        return ResponseEntity.noContent().build();
    }

    @PostMapping("/{lobbyId}/start")
    public ResponseEntity<Lobby> startLobby(@PathVariable int lobbyId, @RequestBody Map<String, Integer> startRequest) {

        int hostUserId = startRequest.get("hostUserId");
        Lobby lobby = lobbyService.startLobby(lobbyId, hostUserId);
        return ResponseEntity.ok(lobby);
    }

    @DeleteMapping("/{lobbyId}")
    public ResponseEntity<Void> deleteLobby(@PathVariable int lobbyId,
            @RequestBody Map<String, Integer> deleteRequest) {
        int userId = deleteRequest.get("userId");
        lobbyService.deleteLobby(lobbyId, userId);
        return ResponseEntity.noContent().build();
    }

    @GetMapping("/{lobbyId}")
    public ResponseEntity<Lobby> getLobbyById(@PathVariable int lobbyId) {
        Lobby lobby = lobbyService.getLobbyById(lobbyId);
        return ResponseEntity.ok(lobby);
    }

    @GetMapping("/code/{code}")
    public ResponseEntity<Lobby> getLobbyByCode(@PathVariable String code) {
        Lobby lobby = lobbyService.getLobbyByCode(code);
        return ResponseEntity.ok(lobby);
    }

    @GetMapping("/active")
    public ResponseEntity<List<Lobby>> getActiveLobbies() {
        return ResponseEntity.ok(lobbyService.getActiveLobbies());
    }

    @GetMapping
    public ResponseEntity<List<Lobby>> getAllLobbies() {
        return ResponseEntity.ok(lobbyService.getAllLobbies());
    }

}
