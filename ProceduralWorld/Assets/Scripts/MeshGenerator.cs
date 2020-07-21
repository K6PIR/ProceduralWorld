﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator {
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve,
        int levelOfDetail) {
        		
	    AnimationCurve heightCurve = new AnimationCurve (_heightCurve.keys);

		int meshSimplificationIncrement = (levelOfDetail == 0)?1:levelOfDetail * 2;

		int borderedSize = heightMap.GetLength (0);
		int meshSize = borderedSize - 2*meshSimplificationIncrement;
		int meshSizeUnsimplified = borderedSize - 2;

		float topLeftX = (meshSizeUnsimplified - 1) / -2f;
		float topLeftZ = (meshSizeUnsimplified - 1) / 2f;


		int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

		MeshData meshData = new MeshData (verticesPerLine);

		int[,] vertexIndicesMap = new int[borderedSize,borderedSize];
		int meshVertexIndex = 0;
		int borderVertexIndex = -1;

		for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
			for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
				bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

				if (isBorderVertex) {
					vertexIndicesMap [x, y] = borderVertexIndex;
					borderVertexIndex--;
				} else {
					vertexIndicesMap [x, y] = meshVertexIndex;
					meshVertexIndex++;
				}
			}
		}

		for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
			for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
				int vertexIndex = vertexIndicesMap [x, y];
				Vector2 percent = new Vector2 ((x-meshSimplificationIncrement) / (float)meshSize, (y-meshSimplificationIncrement) / (float)meshSize);
				float height = heightCurve.Evaluate (heightMap [x, y]) * heightMultiplier;
				Vector3 vertexPosition = new Vector3 (topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);

				meshData.AddVertex (vertexPosition, percent, vertexIndex);

				if (x < borderedSize - 1 && y < borderedSize - 1) {
					int a = vertexIndicesMap [x, y];
					int b = vertexIndicesMap [x + meshSimplificationIncrement, y];
					int c = vertexIndicesMap [x, y + meshSimplificationIncrement];
					int d = vertexIndicesMap [x + meshSimplificationIncrement, y + meshSimplificationIncrement];
					meshData.AddTriangle (a,d,c);
					meshData.AddTriangle (d,a,b);
				}

				vertexIndex++;
			}
		}
		
		return meshData;
    }
}

public class MeshData {
    private Vector3[] _vertices;
    private int[] _triangles;
    private Vector2[] _uvs;

    private Vector3[] _borderVertices;
    private int[] _borderTriangles;
    private int _borderTriangleIndex;

    private int _triangleIndex;

    public MeshData(int verticesPerLine) {
        _vertices = new Vector3[verticesPerLine * verticesPerLine];
        _uvs = new Vector2[verticesPerLine * verticesPerLine];
        _triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

        _borderVertices = new Vector3[verticesPerLine * 4 + 4];
        _borderTriangles = new int[verticesPerLine * 24];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
        if (vertexIndex < 0) {
            _borderVertices[-vertexIndex - 1] = vertexPosition;
        } else {
            _vertices[vertexIndex] = vertexPosition;
            _uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c) {
	    if (a < 0 || b < 0 || c < 0) {
		    _borderTriangles [_borderTriangleIndex] = a;
		    _borderTriangles [_borderTriangleIndex + 1] = b;
		    _borderTriangles [_borderTriangleIndex + 2] = c;
		    _borderTriangleIndex += 3;
	    } else {
		    _triangles [_triangleIndex] = a;
		    _triangles [_triangleIndex + 1] = b;
		    _triangles [_triangleIndex + 2] = c;
		    _triangleIndex += 3;
	    }
    }

    Vector3[] CalculateNormals() {
        Vector3[] vertexNormals = new Vector3[_vertices.Length];
        int triangleCount = _triangles.Length / 3;

        for (int i = 0; i < triangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = _triangles[normalTriangleIndex];
            int vertexIndexB = _triangles[normalTriangleIndex + 1];
            int vertexIndexC = _triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = _borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = _borderTriangles[normalTriangleIndex];
            int vertexIndexB = _borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = _borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            if (vertexIndexA >= 0) vertexNormals[vertexIndexA] += triangleNormal;
            if (vertexIndexB >= 0) vertexNormals[vertexIndexB] += triangleNormal;
            if (vertexIndexC >= 0) vertexNormals[vertexIndexC] += triangleNormal;
        }

        for (int i = 0; i < vertexNormals.Length; i++) {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
        Vector3 pointA = (indexA < 0) ? _borderVertices[-indexA - 1] : _vertices[indexA];
        Vector3 pointB = (indexB < 0) ? _borderVertices[-indexB - 1] : _vertices[indexB];
        Vector3 pointC = (indexC < 0) ? _borderVertices[-indexC - 1] : _vertices[indexC];

        Vector3 ABVector = pointB - pointA;
        Vector3 ACVector = pointC - pointA;

        return Vector3.Cross(ABVector, ACVector).normalized;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = _vertices;
        mesh.triangles = _triangles;
        mesh.uv = _uvs;
        mesh.normals = CalculateNormals();

        return mesh;
    }
}