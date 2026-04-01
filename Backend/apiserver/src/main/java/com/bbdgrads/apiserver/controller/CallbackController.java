package com.bbdgrads.apiserver.controller;

import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.oauth2.core.user.OAuth2User;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

import com.bbdgrads.apiserver.auth.TokenStore;

@RestController
class CallbackController {

    private final TokenStore tokenStore;
    private final String callbackURI = System.getenv("CALLBACK_URI");

    public CallbackController(TokenStore tokenStore) {
        this.tokenStore = tokenStore;
    }

    @GetMapping("/cli-callback")
    public ResponseEntity<Void> cliCallback(@AuthenticationPrincipal OAuth2User user) {
        String email = user.getAttribute("email");
        String token = tokenStore.generateToken(email);
        String constructedCallbackURL = "%scallback?token=".formatted(callbackURI) + token + "&email=" + email;

        return ResponseEntity.status(302)
                .header("Location", constructedCallbackURL)
                .build();
    }
}
