package com.bbdgrads.apiserver.repository;

import java.util.List;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import com.bbdgrads.apiserver.model.User;

@Repository
public interface UserRepository extends JpaRepository<User, Integer> {

    List<User> findByLobbyId(int lobbyId);

    boolean existsByName(String name);

    @Query(value = "SELECT * FROM \"Users\" WHERE name = :name LIMIT 1", nativeQuery = true)
    User findByName(@Param("name") String name);

    @Query(value = "SELECT * FROM \"Users\" WHERE email = :email LIMIT 1", nativeQuery = true)
    User findByEmail(@Param("email") String email);

}
