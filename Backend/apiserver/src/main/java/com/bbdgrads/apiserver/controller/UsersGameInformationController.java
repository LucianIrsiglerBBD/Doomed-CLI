package com.bbdgrads.apiserver.controller;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.bbdgrads.apiserver.dto.UsersGameInfoCreateRequest;
import com.bbdgrads.apiserver.dto.UsersGameInfoUpdateRequest;
import com.bbdgrads.apiserver.model.UsersGameInformation;
import com.bbdgrads.apiserver.services.UsersGameInfoService;

import lombok.RequiredArgsConstructor;

@RestController
@RequestMapping("api/users-game-information")
@RequiredArgsConstructor
public class UsersGameInformationController {

        private final UsersGameInfoService usersGameInfoService;

        @GetMapping("/user/{userId}/lobby/{lobbyId}")
        public ResponseEntity<UsersGameInformation> getUsersGameInformation(
                        @PathVariable int userId,
                        @PathVariable int lobbyId) {

                return ResponseEntity.ok(
                                usersGameInfoService.getUsersGameInformation(userId, lobbyId));
        }

        @PutMapping
        public ResponseEntity<UsersGameInformation> updateUsersGameInformation(
                        @RequestBody UsersGameInfoUpdateRequest req) {

                UsersGameInformation updated = usersGameInfoService.updateUsersGameInformation(
                                req.getUserId(),
                                req.getLobbyId(),
                                req.getSpeed(),
                                req.getHealth(),
                                req.getX(),
                                req.getY());

                return ResponseEntity.ok(updated);
        }

        @PostMapping
        public ResponseEntity<UsersGameInformation> createUsersGameInformation(
                        @RequestBody UsersGameInfoCreateRequest req) {

                UsersGameInformation created = usersGameInfoService.createUsersGameInformation(
                                req.getUserId(),
                                req.getLobbyId(),
                                req.getSpeed(),
                                req.getX(),
                                req.getY());

                return new ResponseEntity<>(created, HttpStatus.CREATED);
        }
}