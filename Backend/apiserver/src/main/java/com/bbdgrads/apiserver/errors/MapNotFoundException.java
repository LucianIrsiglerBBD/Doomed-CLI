package com.bbdgrads.apiserver.errors;

public class MapNotFoundException extends ResourceNotFoundException {
    public MapNotFoundException(String message) {
        super(message);
    }
}