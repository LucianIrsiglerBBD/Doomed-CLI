package com.bbdgrads.apiserver.controller;

import java.util.List;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.bbdgrads.apiserver.dto.CreateLobbyRequest;
import com.bbdgrads.apiserver.dto.JoinLobbyRequest;
import com.bbdgrads.apiserver.dto.StartLobbyRequest;
import com.bbdgrads.apiserver.model.Lobby;
import com.bbdgrads.apiserver.model.UsersGameInformation;
import com.bbdgrads.apiserver.services.LobbyService;
import com.bbdgrads.apiserver.services.UserService;

import lombok.RequiredArgsConstructor;

@RestController
@RequestMapping("api/lobbies")
@RequiredArgsConstructor
public class LobbyController {

    private final LobbyService lobbyService;
    private final UserService userService;

    @PostMapping
    public ResponseEntity<String> createLobby(@RequestBody CreateLobbyRequest request) {

        try{
            var user = userService.getUserByName(request.getUsername());

            if (user == null) {
                return ResponseEntity.status(HttpStatus.NOT_FOUND).body("User with username " + request.getUsername() + " not found");
            }
            Lobby lobby = lobbyService.createLobby(user.getId(), request.getMapId(), request.getDurationInMinutes());
            var gameCode = lobby.getCode();
            return ResponseEntity.ok(gameCode);
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body("An error occurred while creating the lobby");
        }
    }

    @PostMapping("/{lobbyCode}/join")
       public ResponseEntity<UsersGameInformation> joinLobby(
            @PathVariable String lobbyCode,
            @RequestBody JoinLobbyRequest request) {
        UsersGameInformation info = lobbyService.joinLobby(request.getUserId(), lobbyCode);
        return new ResponseEntity<>(info, HttpStatus.CREATED);
    }

    @PostMapping("/{lobbyId}/users/{userId}/leave")
    public ResponseEntity<Void> leaveLobby(@PathVariable int lobbyId, @PathVariable int userId) {
        lobbyService.leaveLobby(userId, lobbyId);
        return ResponseEntity.noContent().build();
    }

    @PostMapping("/{lobbyId}/start")
    public ResponseEntity<Lobby> startLobby(@PathVariable int lobbyId, @RequestBody StartLobbyRequest request) {
        Lobby lobby = lobbyService.startLobby(lobbyId, request.getHostUserId());
        return ResponseEntity.ok(lobby);
    }

    @DeleteMapping("/{lobbyId}/users/{userId}")
    public ResponseEntity<Void> deleteLobby(@PathVariable int lobbyId, @PathVariable int userId) {
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
