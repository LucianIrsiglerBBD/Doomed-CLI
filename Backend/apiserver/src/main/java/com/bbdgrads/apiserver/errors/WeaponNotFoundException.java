package com.bbdgrads.apiserver.errors;

public class WeaponNotFoundException extends ResourceNotFoundException {
    public WeaponNotFoundException(String message) {
        super(message);
    }
}
