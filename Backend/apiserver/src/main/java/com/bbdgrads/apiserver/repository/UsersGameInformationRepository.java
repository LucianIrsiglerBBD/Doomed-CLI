package com.bbdgrads.apiserver.repository;

import java.util.List;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;
import com.bbdgrads.apiserver.model.UsersGameInformation;

@Repository
public interface UsersGameInformationRepository extends JpaRepository<UsersGameInformation, Integer> {

  UsersGameInformation findByUserIdAndLobbyId(int userId, int lobbyId);

  boolean existsByUserIdAndLobbyId(int userId, int lobbyId);

  void deleteByLobbyAndUserId(int lobbyId, int userId);

  int countByLobbyId(int lobbyId);

  List<UsersGameInformation> findByLobbyId(int lobbyId);

  List<UsersGameInformation> findByUserId(int userId);

}
