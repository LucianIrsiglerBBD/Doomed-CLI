package com.bbdgrads.apiserver.errors;

public class UsersGameInformationNotFoundException
        extends ResourceNotFoundException {
    public UsersGameInformationNotFoundException(String message) {
        super(message);
    }
}
