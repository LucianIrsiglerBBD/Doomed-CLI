package com.bbdgrads.apiserver.controller;

import java.util.List;
import java.util.Map;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import com.bbdgrads.apiserver.model.Lobby;
import com.bbdgrads.apiserver.model.User;
import com.bbdgrads.apiserver.model.UsersGameInformation;
import com.bbdgrads.apiserver.services.UserService;
import lombok.RequiredArgsConstructor;

@RestController
@RequestMapping("api/users")
@RequiredArgsConstructor
public class UserController {

    private final UserService userService;

    @GetMapping("/{userId}")
    public ResponseEntity<User> getUserById(@PathVariable int userId) {
        return ResponseEntity.ok(userService.getUserById(userId));
    }

    @GetMapping("/name/{name}")
    public ResponseEntity<User> getUserByName(@PathVariable String name) {
        return ResponseEntity.ok(userService.getUserByName(name));
    }

    @GetMapping
    public ResponseEntity<List<User>> getAllUsers() {
        return ResponseEntity.ok(userService.getAllUsers());
    }

    @DeleteMapping("/{userId}")
    public ResponseEntity<Void> deleteUser(@PathVariable int userId) {
        userService.deleteUser(userId);
        return ResponseEntity.noContent().build();
    }

    @GetMapping("/{userId}/lobby")
    public ResponseEntity<Lobby> getUserLobby(@PathVariable int userId) {
        return ResponseEntity.ok(userService.getUserLobby(userId));
    }

    @PostMapping("/{userId}/lobbies/join")
    public ResponseEntity<?> joinLobby(@PathVariable int userId, @RequestBody Map<String, String> joinRequest) {
        String lobbyCode = joinRequest.get("lobbyCode");
        return ResponseEntity.ok(userService.joinLobby(userId, lobbyCode));
    }

    @PostMapping("/{userId}/lobbies/leave")
    public ResponseEntity<Void> leaveLobby(@PathVariable int userId) {
        userService.leaveLobby(userId);
        return ResponseEntity.noContent().build();
    }

    @PutMapping("/{userId}/lobbies/{lobbyId}/position")
    public ResponseEntity<UsersGameInformation> updatePosition(
            @PathVariable int userId,
            @PathVariable int lobbyId,
            @RequestBody Map<String, Integer> positionRequest) {

        int x = positionRequest.get("x");
        int y = positionRequest.get("y");

        return ResponseEntity.ok(userService.updatePosition(userId, lobbyId, x, y));
    }

    @PutMapping("/{userId}/lobbies/{lobbyId}/health")
    public ResponseEntity<UsersGameInformation> updateHealth(
            @PathVariable int userId,
            @PathVariable int lobbyId,
            @RequestBody Map<String, Integer> healthRequest) {

        int health = healthRequest.get("health");
        return ResponseEntity.ok(userService.updateHealth(userId, lobbyId, health));
    }

    @GetMapping("/{userId}/active-lobby")
    public ResponseEntity<Boolean> hasActiveLobby(@PathVariable int userId) {
        return ResponseEntity.ok(userService.hasActiveLobby(userId));
    }

}