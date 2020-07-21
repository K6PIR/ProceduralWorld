﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {

    public enum NormalizeMode {
        Local,
        Global
    }
    public static float[,] GenerateNoiseMap(int width, int height, int seed, float scale, int octaves, float persistence,
        float lacunarity, Vector2 offset, NormalizeMode normalizeMode) {
        float[,] noiseMap = new float[width, height];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;
        
        
        for (int i = 0; i < octaves; i++) {
            float offsetX = prng.Next(-100000, 1000000) + offset.x;
            float offsetY= prng.Next(-100000, 1000000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistence;
        }
        
        if (scale <= 0) scale = 0.0001f;

        float minLocalNoiseHeight = float.MaxValue;
        float maxLocalNoiseHeight = float.MinValue;
        
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {

                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;
                
                for (int i = 0; i < octaves; i++) {
                    float sampleY = (y - height / 2 + octaveOffsets[i].y) / scale * frequency;
                    float sampleX = (x - width / 2 + octaveOffsets[i].x) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight) maxLocalNoiseHeight = noiseHeight;
                else if (noiseHeight < minLocalNoiseHeight) minLocalNoiseHeight = noiseHeight;
                
                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (normalizeMode == NormalizeMode.Local)
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                else {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        return noiseMap;
    }
}