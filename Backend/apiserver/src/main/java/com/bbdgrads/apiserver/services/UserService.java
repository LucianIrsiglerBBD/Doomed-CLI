package com.bbdgrads.apiserver.services;

import java.util.List;
import org.springframework.stereotype.Service;
import com.bbdgrads.apiserver.errors.LobbyNotFoundException;
import com.bbdgrads.apiserver.errors.UserNotFoundException;
import com.bbdgrads.apiserver.errors.ValidationException;
import com.bbdgrads.apiserver.errors.UsersGameInformationNotFoundException;
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

  
    public User getUserById(int id) {
        return userRepository.findById(id)
                .orElseThrow(() -> new UserNotFoundException("User with id " + id + " not found"));
    }

    public User getUserByName(String name) {
        return userRepository.findByName(name);
                
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
                .orElseThrow(() -> new LobbyNotFoundException(
                        "Lobby with id " + user.getLobby().getId() + " not found"));
    }

 
    public UsersGameInformation joinLobby(int userId, String lobbyCode) {

        User user = getUserById(userId);

        if (user.getLobby() != null) {
            throw new ValidationException("User is already in a lobby");
        }

        Lobby lobby = lobbyRepository.findByCode(lobbyCode);
        if (lobby == null) {
            throw new LobbyNotFoundException("Lobby with code '" + lobbyCode + "' not found");
        }

        if (lobby.isStarted()) {
            throw new ValidationException("Lobby has already started");
        }

        if (usersGameInformationRepository.existsByUserIdAndLobbyId(userId, lobby.getId())) {
            throw new ValidationException("User already has game info for this lobby");
        }

     
        int startX = 0;
        int startY = 0;
        int speed = 10;

        UsersGameInformation info = new UsersGameInformation();
        info.setUser(user);
        info.setLobby(lobby);
        info.setSpeed(speed);
        info.setHealth(100);
        info.setX(startX);
        info.setY(startY);

        UsersGameInformation saved = usersGameInformationRepository.save(info);

        user.setLobby(lobby);
        userRepository.save(user);

        return saved;
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
            
            UsersGameInformation newHostInfo =
                    usersGameInformationRepository.findByLobbyId(lobbyId).get(0);

            User newHost = newHostInfo.getUser();
            lobby.setHostUser(newHost);
            lobbyRepository.save(lobby);
        }
    }



    public UsersGameInformation updatePosition(int userId, int lobbyId, int x, int y) {

        UsersGameInformation info = getUserGameInformation(userId, lobbyId);

        // Basic coordinate validation
        if (x < 0 || y < 0) {
            throw new ValidationException("Coordinates cannot be negative");
        }

        double width = info.getLobby().getMap().getWidth();
        double height = info.getLobby().getMap().getHeight();

        if (x >= width || y >= height) {
            throw new ValidationException("Coordinates out of map bounds");
        }

        info.setX(x);
        info.setY(y);

        return usersGameInformationRepository.save(info);
    }



    public UsersGameInformation updateHealth(int userId, int lobbyId, int health) {

        UsersGameInformation info = getUserGameInformation(userId, lobbyId);

        if (health < 0) health = 0;
        if (health > 100) health = 100;

        info.setHealth(health);

        if (health == 0) {
            handlePlayerDeath(userId, lobbyId);
        }

        return usersGameInformationRepository.save(info);
    }

    private void handlePlayerDeath(int userId, int lobbyId) {
        leaveLobby(userId);
    }


    private UsersGameInformation getUserGameInformation(int userId, int lobbyId) {
        UsersGameInformation info =
                usersGameInformationRepository.findByUserIdAndLobbyId(userId, lobbyId);

        if (info == null) {
            throw new UsersGameInformationNotFoundException(
                    "UserGameInfo for user " + userId + " and lobby " + lobbyId + " not found");
        }

        return info;
    }


    public boolean hasActiveLobby(int userId) {
        User user = getUserById(userId);
        return user.getLobby() != null;
    }

    public List<UsersGameInformation> getUsersInLobby(int lobbyId) {
        return usersGameInformationRepository.findByLobbyId(lobbyId);
    }

    public void deleteUser(int userId) {
        userRepository.deleteById(userId);
    }
}