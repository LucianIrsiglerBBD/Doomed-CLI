package com.bbdgrads.apiserver.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;
import com.bbdgrads.apiserver.model.Lobby;

@Repository
public interface LobbyRepository extends JpaRepository<Lobby, Integer> {

    Lobby findByCode(String code);

}
