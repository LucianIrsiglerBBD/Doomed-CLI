package com.bbdgrads.apiserver.controller;

import java.util.List;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import com.bbdgrads.apiserver.model.GameMap;
import com.bbdgrads.apiserver.services.GameMapService;
import lombok.RequiredArgsConstructor;

@RestController
@RequestMapping("api/maps")
@RequiredArgsConstructor
public class GameMapController {

    private final GameMapService mapService;

    @PostMapping
    public ResponseEntity<GameMap> createMap(@RequestBody GameMap createMapReq) {
        return new ResponseEntity<>(mapService.createMap(createMapReq), HttpStatus.CREATED);
    }

    @GetMapping("/{mapId}")
    public ResponseEntity<GameMap> getMapById(@PathVariable int mapId) {
        return ResponseEntity.ok(mapService.getMapById(mapId));
    }

    @GetMapping("/name/{mapName}")
    public ResponseEntity<GameMap> getMapByName(@PathVariable String mapName) {
        return ResponseEntity.ok(mapService.getMapByName(mapName));
    }

    @GetMapping
    public ResponseEntity<List<GameMap>> getAllMaps() {
        return ResponseEntity.ok(mapService.getAllMaps());
    }

    @DeleteMapping("/{mapId}")
    public ResponseEntity<Void> deleteMap(@PathVariable int mapId) {
        mapService.deleteMap(mapId);
        return ResponseEntity.noContent().build();
    }

    @PutMapping("/{mapId}")
    public ResponseEntity<GameMap> updateMap(@PathVariable int mapId, @RequestBody GameMap mapUpdateReq) {
        return ResponseEntity.ok(mapService.updateMap(mapId, mapUpdateReq));
    }
}