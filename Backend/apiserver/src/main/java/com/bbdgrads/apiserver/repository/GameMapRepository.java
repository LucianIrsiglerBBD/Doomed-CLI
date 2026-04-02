package com.bbdgrads.apiserver.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;
import com.bbdgrads.apiserver.model.GameMap;

@Repository
public interface GameMapRepository extends JpaRepository<GameMap, Integer> {
    GameMap findByName(String name);

    boolean existsByName(String name);
}
