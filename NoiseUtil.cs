using System;
using UnityEngine;

public static class NoiseUtil {
  // Written by Fredric Marqvardt
  //https://github.com/fmarqvardt
  //MIT-license
  
  /// <summary>
  /// Generates and returns a Perlin-Noise Texture2D.
  /// </summary>
  /// <param name="scale">Scale of the sampling area</param>
  /// <param name="removeBias">Remove the inherent positive bias of Unity's Mathf.PerlinNoise-function</param>
  public static Texture2D GeneratePerlinTexture2D(int width, int height, float scale = 6.5f, bool removeBias = true) {
    var samples = GeneratePerlinValues(width, height, scale, removeBias);
    var colors = new Color[samples.Length];
    
    float sample = 0f;
    for (int i = 0; i < colors.Length; i++) {
      sample = samples[i];
      colors[i] = new Color(sample, sample, sample);
    }
    
    Texture2D perlinTexture2D = new Texture2D(width,height,TextureFormat.ARGB4444, false);
    perlinTexture2D.SetPixels(colors);
    perlinTexture2D.Apply(false, false);
    return perlinTexture2D;
  }
  /// <summary>
  /// Generates and returns a Fractal Brownian Motion Texture2D
  /// </summary>
  /// <param name="scale">Scale of the sampling area</param>
  /// <param name="octaves">Number of octaves</param>
  /// <param name="lacunarity">Drives the fall-of of each octave frequency. Recommended 0-1f value.</param>
  /// <param name="persistance">Drives the fall-of of each octave amplitude. Recommended 0-1f value.</param>
  /// <param name="removeBias">Remove the inherent positive bias of Unity's Mathf.PerlinNoise</param>
  public static Texture2D GenerateFbmTexture2D(int width, int height, float scale = 6.5f, int octaves = 3, float lacunarity = 2.4f, float persistance = 0.3f, bool removeBias = true) {
    var samples = GenerateFbmValues(width, height, scale, octaves, lacunarity, persistance, removeBias);
    var colors = new Color[width*height];
      
    float sample = 0f;
    for (int i = 0; i < width*height; i++) {
      sample = samples[i];
      colors[i] = new Color(sample, sample, sample);
    }
    
    Texture2D fbmTexture2D = new Texture2D(width,height,TextureFormat.ARGB4444, false);
    fbmTexture2D.SetPixels(colors);
    fbmTexture2D.Apply(false, false);
    return fbmTexture2D;
  }

  /// <summary>
  /// Generates and returns a float array with Perlin-Noise values.
  /// </summary>
  /// <param name="scale">Scale of the sampling area</param>
  /// <param name="removeBias">Remove the inherent positive bias of Unity's Mathf.Perlin-function</param>
  public static float[] GeneratePerlinValues(int width, int height, float scale = 6.5f, bool removeBias = true) {
    var samples = new float[width * height];
    var origin = 500f;
    
    int x = 0 , y = 0;
    for (int i = 0; i < width*height; i++) {
      IndexToCoordinates(out x, out y, i, width);
      
      float xCoord = origin + (float)x / (float)width * scale;
      float yCoord = origin + (float)y / (float)height * scale;
      float sample = Mathf.PerlinNoise(xCoord, yCoord);
      samples[i] = removeBias ? (sample-0.5f) : sample;
    }
    return samples;
  }

  /// <summary>
  /// Generates and returns a float array with Fractal Brownian Motion values.
  /// </summary>
  /// <param name="scale">Scale of the sampling area</param>
  /// <param name="octaves">Number of octaves</param>
  /// <param name="lacunarity">Drives the fall-of of each octave frequency. Recommended 0-1f value.</param>
  /// <param name="persistance">Drives the fall-of of each octave amplitude. Recommended 0-1f value.</param>
  /// <param name="removeBias">Remove the inherent positive bias of Unity's Mathf.PerlinNoise-function</param>
  public static float[] GenerateFbmValues(int width, int height, float scale = 6.5f, int octaves = 3, float lacunarity = 2.4f, float persistance = 0.3f, bool removeBias = true) {
    var samples = new float[width * height];
    var origin = 500f;

    int x = 0 , y = 0;
    for (int i = 0; i < width*height; i++) {
      float frequency = 1;
      float amplitude = 1;
      IndexToCoordinates(out x, out y, i, width);
      
      for (int j = 0; j < octaves; j++) {
        float xCoord = origin + (float)x / (float)width * scale * frequency;
        float yCoord = origin + (float)y / (float)height * scale * frequency;
        float sample = Mathf.PerlinNoise(xCoord, yCoord);
        samples[i] += (sample-(removeBias ? 0.5f : 0f)) * amplitude;

        amplitude *= persistance;
        frequency *= lacunarity;
      }
    }
    return samples;
  }
  
  /// <summary>
  /// Distorts sourceImage xy-coordinate by the corresponding noiseValue * distortAmount.
  /// The dimensions of noiseValues and sourceImage must match.
  /// </summary>
  /// <returns>Newly generated texture 2D</returns>
  public static Texture2D DistortImage(float[] noiseValues, Texture2D sourceImage, float distortAmount = 50f ) {
    return DistortImage(sourceImage.width, sourceImage.height, noiseValues, sourceImage.GetPixels(), distortAmount);
  }

  /// <summary>
  /// Distorts sourceImage xy-coordinate by the corresponding noiseValue * distortAmount.
  /// The dimensions/array-length of noiseValues, sourceImage and width*height must match.
  /// </summary>
  /// <returns>Newly generated texture 2D</returns>
  public static Texture2D DistortImage(int width, int height, float[] noiseValues, Color[] sourceImage, float distortAmount = 50f) {
    if (noiseValues.Length != sourceImage.Length) {
      Debug.LogWarning("Distort Image Failed: noiseValues and sourceImage length must match.");
      return null;
    }
    Array.Reverse(sourceImage);
    var distorted = new Color[width * height];
    
    //Apply distortion
    int x = 0, y = 0;
    for (int i = 0; i < height*width; i++) {
      NoiseUtil.IndexToCoordinates(out x, out y, i, width);
      float noiseVal = noiseValues[i];
      int shift = Mathf.RoundToInt(noiseVal * distortAmount);
      distorted[i] =
        sourceImage[
          NoiseUtil.CoordinatesToIndex(Mathf.Clamp((x + shift), 0, width - 1),
            Mathf.Clamp((y + shift), 0, height - 1), width)];

    }

    Texture2D distortedTexture2D = new Texture2D(width,height,TextureFormat.ARGB4444, false);
    distortedTexture2D.SetPixels(distorted);
    distortedTexture2D.Apply();
    return distortedTexture2D;
  }

  static int CoordinatesToIndex(int x, int y, int width) {
    return width * y + x;
  }

  static void IndexToCoordinates(out int x, out int y, int index, int width) {
    x = index % width;
    y = index / width;
  }

}




