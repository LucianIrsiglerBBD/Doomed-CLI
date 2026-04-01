package com.bbdgrads.apiserver.controller;

import java.util.List;
import java.util.Map;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.bbdgrads.apiserver.auth.TokenStore;
import com.bbdgrads.apiserver.errors.UserNotFoundException;
import com.bbdgrads.apiserver.errors.ValidationException;
import com.bbdgrads.apiserver.model.Lobby;
import com.bbdgrads.apiserver.model.User;
import com.bbdgrads.apiserver.services.UserService;

import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;

@RestController
@RequestMapping("api/users")
@RequiredArgsConstructor
public class UserController {

    private final UserService userService;
    private final TokenStore tokenStore;

    @GetMapping("/me")
    public ResponseEntity<?> getUsername(HttpServletRequest request) {
        String authHeader = request.getHeader("Authorization");
        
        if (authHeader == null || !authHeader.startsWith("Bearer ")) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).body("Missing or invalid authorization token");
        }
        
        String token = authHeader.substring(7);
        String email = tokenStore.getEmail(token);
        
        if (email == null) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).body("Invalid or expired token");
        }
        
        User user = userService.getUserByEmail(email);
        if (user == null) {
            return ResponseEntity.status(HttpStatus.NOT_FOUND).body("User not found");
        }

        Integer lobbyId = user.getLobby() != null ? user.getLobby().getId() : null;
        return ResponseEntity.ok(Map.of(
                "id", user.getId(),
                "username", user.getName(),
                "lobbyId", lobbyId != null ? lobbyId : -1));
    }

    @PatchMapping("/")
    public ResponseEntity<String> updateUsername(@RequestBody Map<String, String> userRequest) {
        String email = userRequest.get("email");
        String name = userRequest.get("username");

        try{
            userService.updateUsernameByEmail(name, email);
            return ResponseEntity.ok("Username updated successfully");
        } catch (UserNotFoundException e) {
            return ResponseEntity.status(HttpStatus.NOT_FOUND).body("User with email " + email + " not found");
        } catch (ValidationException e){
            return ResponseEntity.status(HttpStatus.BAD_REQUEST).body("Username is already taken");
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).build();
        }
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

    @GetMapping("/{userId}/active-lobby")
    public ResponseEntity<Boolean> hasActiveLobby(@PathVariable int userId) {
        return ResponseEntity.ok(userService.hasActiveLobby(userId));
    }

}