package com.bbdgrads.apiserver.services;

import java.util.List;
import java.util.Optional;
import org.springframework.stereotype.Service;
import com.bbdgrads.apiserver.errors.LobbyNotFoundException;
import com.bbdgrads.apiserver.errors.UserNotFoundException;
import com.bbdgrads.apiserver.errors.ValidationException;
import com.bbdgrads.apiserver.model.Lobby;
import com.bbdgrads.apiserver.model.User;
import com.bbdgrads.apiserver.model.UsersGameInformation;
import com.bbdgrads.apiserver.repository.LobbyRepository;
import com.bbdgrads.apiserver.repository.UserRepository;
import com.bbdgrads.apiserver.repository.UsersGameInformationRepository;
import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;

@Service
@RequiredArgsConstructor
@Transactional
public class UserService {

    private final UserRepository userRepository;
    private final LobbyRepository lobbyRepository;
    private final UsersGameInformationRepository usersGameInformationRepository;
    private final UsersGameInfoService usersGameInfoService; // delegate stats

    public User getUserById(int id) {
        return userRepository.findById(id)
                .orElseThrow(() -> new UserNotFoundException("User with id " + id + " not found"));
    }

    public User getUserByName(String name) {
        return userRepository.findByName(name);
    }

    public User getUserByEmail(String email) {
        return userRepository.findByEmail(email);
    }

    public User updateUsernameByEmail(String name, String email){
        User user = userRepository.findByEmail(email);
        if (user == null) {
            throw new UserNotFoundException("User with email " + email + " not found");
        }

        if (userRepository.existsByName(name)) {
            throw new ValidationException("Username '" + name + "' is already taken");
        }

        user.setName(name);
        return userRepository.save(user);
    }

    public List<User> getAllUsers() {
        return userRepository.findAll();
    }

    public Lobby getUserLobby(int userId) {
        User user = getUserById(userId);
        if (user.getLobby() == null) {
            throw new LobbyNotFoundException("User " + userId + " is not in any lobby");
        }
        return lobbyRepository.findById(user.getLobby().getId())
                .orElseThrow(() -> new LobbyNotFoundException("Lobby with id " + user.getLobby().getId() + " not found"));
    }

    public UsersGameInformation joinLobby(int userId, String lobbyCode) {
        User user = getUserById(userId);
        if (user.getLobby() != null) {
            throw new ValidationException("User is already in a lobby");
        }

        Lobby lobby = Optional.ofNullable(lobbyRepository.findByCode(lobbyCode))
                .orElseThrow(() -> new LobbyNotFoundException("Lobby with code '" + lobbyCode + "' not found"));

        if (lobby.isStarted()) {
            throw new ValidationException("Lobby has already started");
        }

        user.setLobby(lobby);
        userRepository.save(user);

        return usersGameInfoService.createUsersGameInformation(userId, lobby.getId(), 10, 0, 0);
    }

    public void leaveLobby(int userId) {
        User user = getUserById(userId);
        if (user.getLobby() == null) {
            throw new ValidationException("User is not in any lobby");
        }

        int lobbyId = user.getLobby().getId();
        Lobby lobby = lobbyRepository.findById(lobbyId)
                .orElseThrow(() -> new LobbyNotFoundException("Lobby with id " + lobbyId + " not found"));

        usersGameInformationRepository.deleteByLobbyAndUserId(lobbyId, userId);

        user.setLobby(null);
        userRepository.save(user);

        int remaining = usersGameInformationRepository.countByLobbyId(lobbyId);

        if (remaining == 0) {
            lobbyRepository.delete(lobby);
        } else if (lobby.getHostUser().getId().equals(userId)) {
            UsersGameInformation newHostInfo = usersGameInformationRepository.findByLobbyId(lobbyId).get(0);
            User newHost = newHostInfo.getUser();
            lobby.setHostUser(newHost);
            lobbyRepository.save(lobby);
        }
    }

    public boolean hasActiveLobby(int userId) {
        return getUserById(userId).getLobby() != null;
    }

    public List<UsersGameInformation> getUsersInLobby(int lobbyId) {
        return usersGameInformationRepository.findByLobbyId(lobbyId);
    }

    public void deleteUser(int userId) {
        userRepository.deleteById(userId);
    }
}
