package com.bbdgrads.apiserver.repository;

import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;

import com.bbdgrads.apiserver.model.CliToken;

public interface CliTokenRepository extends JpaRepository<CliToken, String> {
    Optional<CliToken> findByToken(String token);
    Optional<CliToken> findByUserId(int userId);
}