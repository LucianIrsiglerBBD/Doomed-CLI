package com.bbdgrads.apiserver.services;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Random;
import java.util.concurrent.ThreadLocalRandom;

import org.springframework.stereotype.Service;
import com.bbdgrads.apiserver.errors.LobbyNotFoundException;
import com.bbdgrads.apiserver.errors.MapNotFoundException;
import com.bbdgrads.apiserver.errors.UserNotFoundException;
import com.bbdgrads.apiserver.errors.ValidationException;
import com.bbdgrads.apiserver.model.Lobby;
import com.bbdgrads.apiserver.model.GameMap;
import com.bbdgrads.apiserver.model.User;
import com.bbdgrads.apiserver.model.UsersGameInformation;
import com.bbdgrads.apiserver.repository.LobbyRepository;
import com.bbdgrads.apiserver.repository.GameMapRepository;
import com.bbdgrads.apiserver.repository.UserRepository;
import com.bbdgrads.apiserver.repository.UsersGameInformationRepository;

import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;

@Service
@RequiredArgsConstructor

public class LobbyService {

    private final LobbyRepository lobbyRepository;
    private final UserRepository userRepository;
    private final GameMapRepository mapRepository;
    private final UsersGameInformationRepository usersGameInformationRepository;
    private static final String CHARACTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static final int CODE_LENGTH = 6;
    private static final int MAX_PLAYERS = 50;

    public Lobby createLobby(int hostUserId, int mapId, int durationMinutes) {

        User host = userRepository.findById(hostUserId)
                .orElseThrow(() -> new UserNotFoundException("Host with ID " + hostUserId + " not found"));

        if (host.getLobby() != null) {
            throw new RuntimeException("Host is already in a lobby");
        }

        GameMap map = mapRepository.findById(mapId)
                .orElseThrow(() -> new MapNotFoundException("Map with id " + mapId + " not found"));

        String lobbyCode;
        do {
            lobbyCode = generateLobbyCode();
        } while (lobbyRepository.findByCode(lobbyCode) != null);

        Lobby lobby = new Lobby();
        lobby.setHostUser(host);
        lobby.setMap(map);
        lobby.setCode(lobbyCode);
        lobby.setStarted(false);

        LocalDateTime now = LocalDateTime.now();
        lobby.setCreatedAt(now);
        lobby.setStartTime(now);
        lobby.setEndTime(now.plusMinutes(durationMinutes));

        Lobby savedLobby = lobbyRepository.save(lobby);

        joinLobby(hostUserId, lobbyCode);

        return savedLobby;
    }

    private String generateLobbyCode() {
        StringBuilder code = new StringBuilder(CODE_LENGTH);
        for (int i = 0; i < CODE_LENGTH; i++) {
            code.append(CHARACTERS.charAt(ThreadLocalRandom.current().nextInt(CHARACTERS.length())));
        }
        return code.toString();
    }

    @Transactional
    public UsersGameInformation joinLobby(int userId, String lobbyCode) {

        User user = userRepository.findById(userId)
                .orElseThrow(() -> new UserNotFoundException("User with id " + userId + " not found"));

        if (user.getLobby() != null) {
            throw new RuntimeException("User is already in a lobby");
        }

        Lobby lobby = lobbyRepository.findByCode(lobbyCode);
        if (lobby == null) {
            throw new LobbyNotFoundException("Lobby with code " + lobbyCode + " not found");
        }

        if (lobby.isStarted()) {
            throw new ValidationException("Cannot join, game already started");
        }

        int currentPlayers = usersGameInformationRepository.countByLobbyId(lobby.getId());
        if (currentPlayers >= MAX_PLAYERS) {
            throw new ValidationException("Lobby is full");
        }

        if (usersGameInformationRepository.existsByUserIdAndLobbyId(userId, lobby.getId())) {
            throw new ValidationException("User already has game info for this lobby");
        }

        UsersGameInformation info = new UsersGameInformation();

        info.setHealth(100);
        info.setSpeed(10);
        info.setX(0);
        info.setY(0);
        info.setUser(user);
        info.setLobby(lobby);

        UsersGameInformation saved = usersGameInformationRepository.save(info);

        user.setLobby(lobby);
        userRepository.save(user);

        return saved;
    }
   
    public Lobby startLobby(int lobbyId, int hostUserId) {
        Lobby lobby = lobbyRepository.findById(lobbyId)
                .orElseThrow(() -> new LobbyNotFoundException("Lobby with id " + lobbyId + " not found"));

        if (!lobby.getHostUser().getId().equals(hostUserId)) {
            throw new RuntimeException("Only host can start game");
        }

        if (lobby.isStarted()) {
            throw new RuntimeException("Game already started");
        }

        int playerCount = usersGameInformationRepository.countByLobbyId(lobbyId);
        if (playerCount < 2) {
            throw new RuntimeException("At least 2 players required to start game");
        }

        lobby.setStarted(true);
        lobby.setStartTime(LocalDateTime.now());
        return lobbyRepository.save(lobby);
    }

    public Lobby endLobby(int lobbyId) {
        Lobby lobby = getLobbyById(lobbyId);
        lobby.setStarted(false);
        lobby.setEndTime(LocalDateTime.now());
        return lobbyRepository.save(lobby);
    }

    public Lobby getLobbyById(int lobbyId) {
        return lobbyRepository.findById(lobbyId)
                .orElseThrow(() -> new LobbyNotFoundException("Lobby with id " + lobbyId + " not found"));
    }

    public Lobby getLobbyByCode(String code) {
        Lobby lobby = lobbyRepository.findByCode(code);
        if (lobby == null) {
            throw new LobbyNotFoundException("Lobby with code " + code + " not found");
        }
        return lobby;
    }

    public List<User> getUsersInLobby(int lobbyId) {
        lobbyRepository.findById(lobbyId)
                .orElseThrow(() -> new LobbyNotFoundException("Lobby with id " + lobbyId + " not found"));

        return userRepository.findByLobbyId(lobbyId);
    }

    @Transactional
    public void deleteLobby(int lobbyId, int userId) {
        Lobby lobby = lobbyRepository.findById(lobbyId)
                .orElseThrow(() -> new LobbyNotFoundException("Lobby with id " + lobbyId + " not found"));

        if (!lobby.getHostUser().getId().equals(userId)) {
            throw new RuntimeException("Only the host can delete the lobby");
        }

        List<UsersGameInformation> players = usersGameInformationRepository.findByLobbyId(lobbyId);

        for (UsersGameInformation player : players) {
            User user = player.getUser();
            user.setLobby(null);
            userRepository.save(user);
        }

        usersGameInformationRepository.deleteAll(players);
        lobbyRepository.delete(lobby);
    }

    public void leaveLobby(int userId, int lobbyId) {

        User user = userRepository.findById(userId)
                .orElseThrow(() -> new UserNotFoundException("User with id " + userId + " not found"));

        Lobby lobby = lobbyRepository.findById(lobbyId)
                .orElseThrow(() -> new LobbyNotFoundException("Lobby with id " + lobbyId + " not found"));

        usersGameInformationRepository.deleteByLobbyAndUserId(lobbyId, userId);

        user.setLobby(null);
        userRepository.save(user);

        int playerCount = usersGameInformationRepository.countByLobbyId(lobbyId);

        if (playerCount == 0) {
            lobbyRepository.delete(lobby);
            return;
        }

        if (lobby.getHostUser().getId().equals(userId)) {
            List<UsersGameInformation> remainingPlayers = usersGameInformationRepository.findByLobbyId(lobbyId);

            if (!remainingPlayers.isEmpty()) {
                User newHost = remainingPlayers.get(0).getUser();
                lobby.setHostUser(newHost);
                lobbyRepository.save(lobby);
            }
        }
    }

    public List<Lobby> getActiveLobbies() {

        return lobbyRepository.findAll().stream().filter(Lobby::isStarted).toList();
    }

    public List<Lobby> getAllLobbies() {
        return lobbyRepository.findAll();
    }
}