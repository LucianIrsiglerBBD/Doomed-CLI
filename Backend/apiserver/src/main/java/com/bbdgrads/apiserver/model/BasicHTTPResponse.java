package com.bbdgrads.apiserver.model;

import org.springframework.http.HttpStatus;

import lombok.Data;

@Data
public class BasicHTTPResponse {
    private HttpStatus httpCode;
    private String message;

    public BasicHTTPResponse(HttpStatus httpCode, String message) {
        this.httpCode = httpCode;
        this.message = message;
    }

}
