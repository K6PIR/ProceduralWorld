using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator {
	
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail) {
        		
		int skipIncrement = (levelOfDetail == 0)?1:levelOfDetail * 2;

		int numVertsPerLine = meshSettings.numVertsPerLine;
		
		Vector2 topLeft = new Vector2(-1, 1) * meshSettings.meshWorldSize / 2f;
		
		MeshData meshData = new MeshData (numVertsPerLine, skipIncrement, meshSettings.useFlatShading);

		int[,] vertexIndicesMap = new int[numVertsPerLine, numVertsPerLine];
		int meshVertexIndex = 0;
		int outOfMeshVertexIndex = -1;

		for (int y = 0; y < numVertsPerLine; y++) {
			for (int x = 0; x < numVertsPerLine; x++) {
				bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
				bool isSkippedVertex = x > 2 && y > 2 && x < numVertsPerLine - 3 && y < numVertsPerLine - 3
					&& ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
				
				if (isOutOfMeshVertex) {
					vertexIndicesMap [x, y] = outOfMeshVertexIndex;
					outOfMeshVertexIndex--;
				} else if (!isSkippedVertex) {
					vertexIndicesMap [x, y] = meshVertexIndex;
					meshVertexIndex++;
				}
			}
		}

		for (int y = 0; y < numVertsPerLine; y++) {
			for (int x = 0; x < numVertsPerLine; x++) {
				bool isSkippedVertex = x > 2 && y > 2 && x < numVertsPerLine - 3 && y < numVertsPerLine - 3
				                       && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
				if (!isSkippedVertex) {
					bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
					bool isMeshEdgeVertex = (y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2) && !isOutOfMeshVertex;
					bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
					bool isEdgeConnectionVertex =
						(y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) &&
						!isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;
					int vertexIndex = vertexIndicesMap[x, y];
					Vector2 percent = new Vector2(x - 1, y - 1) / (numVertsPerLine - 3);
					Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.meshWorldSize;
					
					float height = heightMap[x, y];

					if (isEdgeConnectionVertex) {
						bool isVertical = x == 2 || x == numVertsPerLine - 3;
						
						int dstToMainVertexA = (isVertical ? y - 2 : x - 2 ) % skipIncrement;
						int dstToMainVertexB = skipIncrement - dstToMainVertexA;
						float dstPercentFromAToB = dstToMainVertexA / (float) skipIncrement;

						float heightMainVertexA = heightMap[(isVertical ? x : x - dstToMainVertexA),
							(isVertical ? y - dstToMainVertexA : y)];

						float heightMainVertexB = heightMap[(isVertical ? x : x + dstToMainVertexB),
							(isVertical ? y + dstToMainVertexB : y)];

						height = heightMainVertexA * (1 - dstPercentFromAToB) + heightMainVertexB * dstPercentFromAToB;
					}
					
					meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);

					bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 &&
					                      (!isEdgeConnectionVertex || (x != 2 && y != 2));
					
					if (createTriangle) {
						int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3)
							? skipIncrement
							: 1;
						
						int a = vertexIndicesMap[x, y];
						int b = vertexIndicesMap[x + currentIncrement, y];
						int c = vertexIndicesMap[x, y + currentIncrement];
						int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];
						meshData.AddTriangle(a, d, c);
						meshData.AddTriangle(d, a, b);
					}
				}
			}
		}
		
		meshData.ProcessMesh();
		
		return meshData;
    }
}

public class MeshData {
    private Vector3[] _vertices;
    private int[] _triangles;
    private Vector2[] _uvs;
    private Vector3[] _bakedNormals;

    private Vector3[] _outOfMeshVertices;
    private int[] _outOfMeshTriangles;
    private int _outOfMeshTriangleIndex;

    private int _triangleIndex;

    private bool _useFlatShading;
    
    public MeshData(int numVertsPerLine, int skipIncrement, bool useFlatShading) {
	    _useFlatShading = useFlatShading;

	    int numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;
	    int numEdgeConnectionVertices = (skipIncrement - 1) * (numVertsPerLine - 5) / skipIncrement * 4;
	    int numMainVerticesPerLine = (numVertsPerLine - 5) / skipIncrement + 1;
	    int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;
	    
	    _vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];
        _uvs = new Vector2[_vertices.Length];

        int numMeshEdgeTriangles = (numMainVerticesPerLine - 4) * 8;
        int numMainTriangles = (numVertsPerLine - 1) * (numVertsPerLine - 1) * 2;
        _triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];

        _outOfMeshVertices = new Vector3[numVertsPerLine * 4 - 4];
        _outOfMeshTriangles = new int[(numVertsPerLine - 2) * 24];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
        if (vertexIndex < 0) {
            _outOfMeshVertices[-vertexIndex - 1] = vertexPosition;
        } else {
            _vertices[vertexIndex] = vertexPosition;
            _uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c) {
	    if (a < 0 || b < 0 || c < 0) {
		    _outOfMeshTriangles [_outOfMeshTriangleIndex] = a;
		    _outOfMeshTriangles [_outOfMeshTriangleIndex + 1] = b;
		    _outOfMeshTriangles [_outOfMeshTriangleIndex + 2] = c;
		    _outOfMeshTriangleIndex += 3;
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

        int borderTriangleCount = _outOfMeshTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = _outOfMeshTriangles[normalTriangleIndex];
            int vertexIndexB = _outOfMeshTriangles[normalTriangleIndex + 1];
            int vertexIndexC = _outOfMeshTriangles[normalTriangleIndex + 2];

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
        Vector3 pointA = (indexA < 0) ? _outOfMeshVertices[-indexA - 1] : _vertices[indexA];
        Vector3 pointB = (indexB < 0) ? _outOfMeshVertices[-indexB - 1] : _vertices[indexB];
        Vector3 pointC = (indexC < 0) ? _outOfMeshVertices[-indexC - 1] : _vertices[indexC];

        Vector3 ABVector = pointB - pointA;
        Vector3 ACVector = pointC - pointA;

        return Vector3.Cross(ABVector, ACVector).normalized;
    }

    public void BakeNormals() {
	    _bakedNormals = CalculateNormals();
    }

    public void ProcessMesh() {
	    if (_useFlatShading) {
		    FlatShading();
	    } else {
		    BakeNormals();
	    }
    }
    
    void FlatShading() {
	    Vector3[] flatShadedVertices = new Vector3[_triangles.Length];
	    Vector2[] flatShadedUVs = new Vector2[_triangles.Length];

	    for (int i = 0; i < _triangles.Length; i++) {
		    flatShadedVertices[i] = _vertices[_triangles[i]];
		    flatShadedUVs[i] = _uvs[_triangles[i]];
		    _triangles[i] = i;
	    }

	    _vertices = flatShadedVertices;
	    _uvs = flatShadedUVs;
    }
    
    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = _vertices;
        mesh.triangles = _triangles;
        mesh.uv = _uvs;
        
        if (_useFlatShading) mesh.RecalculateNormals();
        else mesh.normals = _bakedNormals;

        return mesh;
    }
}