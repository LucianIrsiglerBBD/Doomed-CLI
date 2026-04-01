package com.bbdgrads.apiserver.controller;

import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

import com.bbdgrads.apiserver.model.BasicHTTPResponse;

@RestController
class StatusController {

    @GetMapping("/status")
    public BasicHTTPResponse status() {
        return new BasicHTTPResponse(HttpStatus.OK, "Server is responding");
    }

    @GetMapping("/checkGoogleAuth")
    public BasicHTTPResponse checkGoogleAuth() {
        // This endpoint does NOT work without a token for the user
        return new BasicHTTPResponse(HttpStatus.OK, "Google Authentication is working");
    }

}
