package com.bbdgrads.apiserver.errors;

public class LobbyNotFoundException extends ResourceNotFoundException {
    public LobbyNotFoundException(String message) {
        super(message);
    }
}