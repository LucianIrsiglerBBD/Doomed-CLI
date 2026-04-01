package com.bbdgrads.apiserver.services;

import org.springframework.stereotype.Service;

import com.bbdgrads.apiserver.errors.LobbyNotFoundException;
import com.bbdgrads.apiserver.errors.UserNotFoundException;
import com.bbdgrads.apiserver.errors.UsersGameInformationNotFoundException;
import com.bbdgrads.apiserver.errors.ValidationException;
import com.bbdgrads.apiserver.model.Lobby;
import com.bbdgrads.apiserver.model.User;
import com.bbdgrads.apiserver.model.UsersGameInformation;
import com.bbdgrads.apiserver.repository.LobbyRepository;
import com.bbdgrads.apiserver.repository.UserRepository;
import com.bbdgrads.apiserver.repository.UsersGameInformationRepository;

import lombok.RequiredArgsConstructor;

@Service
@RequiredArgsConstructor
public class UsersGameInfoService {

    private final UsersGameInformationRepository userGameInfoRepository;
    private final UserRepository userRepository;
    private final LobbyRepository lobbyRepository;

    public static void validatePosition(Lobby lobby, int x, int y) {
        if (x < 0 || y < 0) {
            throw new ValidationException("Coordinates cannot be negative");
        }
    
    }

    public static void validateStats(int speed, int health) {
        if (speed < 0) {
            throw new ValidationException("Speed cannot be negative");
        }
        if (health < 0 || health > 100) {
            throw new ValidationException("Health must be between 0 and 100");
        }
    }

    public UsersGameInformation createUsersGameInformation(int userId, int lobbyId, int speed, int x, int y) {
        User user = userRepository.findById(userId)
                .orElseThrow(() -> new UserNotFoundException("User with id " + userId + " not found"));
        Lobby lobby = lobbyRepository.findById(lobbyId)
                .orElseThrow(() -> new LobbyNotFoundException("Lobby with id " + lobbyId + " not found"));

        if (userGameInfoRepository.findByUserIdAndLobbyId(userId, lobbyId) != null) {
            throw new ValidationException("User already has game info for this lobby");
        }
        
        validatePosition(lobby, x, y);
        validateStats(speed, 100);

        UsersGameInformation info = new UsersGameInformation();
        info.setUser(user);
        info.setLobby(lobby);
        info.setSpeed(speed);
        info.setHealth(100);
        info.setX(x);
        info.setY(y);

        return userGameInfoRepository.save(info);
    }

    public UsersGameInformation updateUsersGameInformation(int userId, int lobbyId, int speed, int health, int x, int y) {
        UsersGameInformation info = userGameInfoRepository.findByUserIdAndLobbyId(userId, lobbyId);
        if (info == null) {
            throw new UsersGameInformationNotFoundException("UserGameInfo for user " + userId + " and lobby " + lobbyId + " not found");
        }

        validatePosition(info.getLobby(), x, y);
        validateStats(speed, health);

        info.setSpeed(speed);
        info.setHealth(health);
        info.setX(x);
        info.setY(y);


        return userGameInfoRepository.save(info);
    }

    public UsersGameInformation getUsersGameInformation(int userId, int lobbyId) {
        UsersGameInformation info = userGameInfoRepository.findByUserIdAndLobbyId(userId, lobbyId);
        if (info == null) {
            throw new UsersGameInformationNotFoundException("UserGameInfo for user " + userId + " and lobby " + lobbyId + " not found");
        }
        return info;
    }
}
