package com.bbdgrads.apiserver.services;

import java.time.LocalDateTime;
import java.util.List;
import org.springframework.stereotype.Service;
import com.bbdgrads.apiserver.errors.DuplicateResourceException;
import com.bbdgrads.apiserver.errors.MapNotFoundException;
import com.bbdgrads.apiserver.model.GameMap;
import com.bbdgrads.apiserver.repository.GameMapRepository;
import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;

@Service
@RequiredArgsConstructor
@Transactional
public class GameMapService {

    private final GameMapRepository mapRepository;


    public GameMap createMap(GameMap map) {

        if (mapRepository.existsByName(map.getName())) {
            throw new DuplicateResourceException("Map name '" + map.getName() + "' already exists");
        }

        map.setCreatedAt(LocalDateTime.now());
        return mapRepository.save(map);
    }

    
    public List<GameMap> getAllMaps() {
        return mapRepository.findAll();
    }


    public GameMap updateMap(int mapId, GameMap mapUpdateRequest) {
        GameMap existingMap = mapRepository.findById(mapId)
                .orElseThrow(() -> new MapNotFoundException("Map with id" + mapId + " not found"));

        GameMap otherMap = mapRepository.findByName(mapUpdateRequest.getName());
        if (otherMap != null && otherMap.getId() != mapId) {
            throw new DuplicateResourceException("Map name '" + mapUpdateRequest.getName() + "' already exists");
        }

        existingMap.setName(mapUpdateRequest.getName());
        existingMap.setData(mapUpdateRequest.getData());

        return mapRepository.save(existingMap);
    }

 
    public GameMap getMapByName(String name) {
        return mapRepository.findByName(name);
    }

    public void deleteMap(int mapId) {
        GameMap map = mapRepository.findById(mapId)
                .orElseThrow(() -> new MapNotFoundException("Map with id " + mapId + " not found"));
        mapRepository.delete(map);

    }

    public GameMap getMapById(int mapId) {
        return mapRepository.findById(mapId)
                .orElseThrow(() -> new MapNotFoundException("Map with that id" + mapId + " not found"));
    }

}
