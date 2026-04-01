package com.bbdgrads.apiserver.auth;

import java.time.LocalDateTime;

import org.springframework.stereotype.Service;

import com.bbdgrads.apiserver.model.CliToken;
import com.bbdgrads.apiserver.model.User;
import com.bbdgrads.apiserver.repository.CliTokenRepository;
import com.bbdgrads.apiserver.repository.UserRepository;

import lombok.RequiredArgsConstructor;

@Service
@RequiredArgsConstructor
public class TokenStore {

    private final CliTokenRepository cliTokenRepository;
    private final UserRepository userRepository;

    public String generateToken(String email) {
        User user = userRepository.findByEmail(email);
        if (user == null) {
            user = new User();
            user.setEmail(email);
            user.setName("User" + System.currentTimeMillis());
            userRepository.save(user);
        }

        CliToken token = new CliToken();
        token.setCreatedAt(LocalDateTime.now());
        token.setExpiresAt(LocalDateTime.now().plusDays(7)); // expires in 7 days
        token.setUserId(user.getId());
        
        //if the user already has a valid token, return that instead of creating a new one
        return cliTokenRepository.findByUserId(user.getId())
                .filter(t -> !t.isExpired())
                .map(CliToken::getToken)
                .orElseGet(() -> cliTokenRepository.save(token).getToken());

    }

    public String getEmail(String token) {
        return userRepository.findById(cliTokenRepository.findByToken(token)
                .filter(t -> !t.isExpired())
                .map(CliToken::getUserId)
                .orElse(-1))
                .map(User::getEmail)
                .orElse(null);
    }

    public boolean isValid(String token) {
        return getEmail(token) != null;
    }

}