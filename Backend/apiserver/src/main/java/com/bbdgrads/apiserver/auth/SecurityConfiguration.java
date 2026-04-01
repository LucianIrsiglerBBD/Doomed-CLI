package com.bbdgrads.apiserver.auth;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.HttpStatus;
import org.springframework.jdbc.core.JdbcOperations;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.oauth2.client.JdbcOAuth2AuthorizedClientService;
import org.springframework.security.oauth2.client.OAuth2AuthorizedClientService;
import org.springframework.security.oauth2.client.registration.ClientRegistrationRepository;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.HttpStatusEntryPoint;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;

import lombok.RequiredArgsConstructor;

@Configuration
@EnableWebSecurity
@RequiredArgsConstructor
public class SecurityConfiguration {

        private final TokenAuthFilter tokenAuthFilter;

        @Bean
        public SecurityFilterChain filterChain(HttpSecurity http) throws Exception {
                http
                .csrf(csrf -> csrf.ignoringRequestMatchers("/api/**"))
                .authorizeHttpRequests(auth -> auth
                                .requestMatchers( "/login**", "/status", "/logout", "/cli-callback")
                                .permitAll()
                                .anyRequest().authenticated())
                .oauth2Login(oauth2 -> oauth2
                                .defaultSuccessUrl("/cli-callback", true)
                                .failureUrl("/login?error"))
                .addFilterBefore(tokenAuthFilter, UsernamePasswordAuthenticationFilter.class)
                .logout(logout -> logout
                                .logoutUrl("/logout")
                                .logoutSuccessUrl("/")
                                .invalidateHttpSession(true)
                                .clearAuthentication(true)
                                .deleteCookies("JSESSIONID", "SESSION"))
                .exceptionHandling(exceptionHandling -> exceptionHandling
                                .authenticationEntryPoint(
                                                new HttpStatusEntryPoint(HttpStatus.UNAUTHORIZED)));

                return http.build();
        }

        @Bean
        public OAuth2AuthorizedClientService authorizedClientService(
                        JdbcOperations jdbcOperations,
                        ClientRegistrationRepository clientRegistrationRepository) {
                return new JdbcOAuth2AuthorizedClientService(jdbcOperations, clientRegistrationRepository);

        }
}