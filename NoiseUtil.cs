using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using math = Unity.Mathematics.math;

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
    
    Texture2D perlinTexture2D = new Texture2D(width,height,TextureFormat.RGB24, false);
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
    
    Texture2D fbmTexture2D = new Texture2D(width,height,TextureFormat.RGB24, false);
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
    var distorted = new Color[width * height];
    float noiseVal;
    //Apply distortion
    int x = 0, y = 0;
    for (int i = 0; i < height*width; i++) {
      NoiseUtil.IndexToCoordinates(out x, out y, i, width);
      noiseVal = noiseValues[i];
      int shift = Mathf.RoundToInt(noiseVal * distortAmount);
      distorted[i] =
        sourceImage[
          NoiseUtil.CoordinatesToIndex(Mathf.Clamp((x + shift), 0, width - 1),
            Mathf.Clamp((y + shift), 0, height - 1), width)];

    }

    Texture2D distortedTexture2D = new Texture2D(width,height,TextureFormat.RGB24, false);
    distortedTexture2D.SetPixels(distorted);
    distortedTexture2D.Apply();
    return distortedTexture2D;
  }
  
  public static Texture2D DistortImageBurst(int width, int height, NativeArray<float> noiseValues, Color[] sourceImage, float distortAmount = 50f) {
    if (noiseValues.Length != sourceImage.Length) {
      Debug.LogWarning("Distort Image Burst Failed: noiseValues and sourceImage length must match.");
      return null;
    }
    var souceNativeArray = GetNativeArrayColor(sourceImage);
    var distorted = new NativeArray<Color>(width * height, Allocator.TempJob);
    var job = new DistortJob() {samples = noiseValues, height = height, width = width, destination = distorted, source = souceNativeArray, distortAmount = distortAmount}
      .Schedule(distorted.Length, 64);
    job.Complete();

    Texture2D distortedTexture2D = new Texture2D(width,height,TextureFormat.RGB24, false);
    distortedTexture2D.SetPixels(distorted.ToArray());
    distortedTexture2D.Apply();
    
    distorted.Dispose();
    souceNativeArray.Dispose();
    
    return distortedTexture2D;
  }

  public static NativeArray<float> GeneratePerlinValuesBurst(int width, int height, float scale = 6.5f) {
    NativeArray<float> samples = new NativeArray<float>(width * height, Allocator.TempJob);
      var job = new PerlinJob() {samples = samples, height = height, width = width, scale = scale, origin = 500f}
        .Schedule(samples.Length, 64);
      job.Complete();
      return samples;
    }
  
  public static NativeArray<float> GenerateFbmValuesBurst(int width, int height, float scale = 6.5f, int octaves = 3, float lacunarity = 2.4f, float persistance = 0.3f) {
    
    NativeArray<float> samples = new NativeArray<float>(width * height, Allocator.TempJob);
    var job = new FbmJob(){samples = samples, height = height, width = width, scale = scale, 
        origin = 500f, octaves = octaves, lacunarity = lacunarity, persistance = persistance}
      .Schedule(samples.Length, 64);
    job.Complete();
    return samples;
  }
  
  public static Texture2D GenerateFbmTexture2DBurst(int width, int height, float scale = 6.5f, int octaves = 3, float lacunarity = 2.4f, float persistance = 0.3f, bool removeBias = true) {
    var samples = GenerateFbmValuesBurst(width, height, scale, octaves, lacunarity, persistance);
    NativeArray<Color> colors = new NativeArray<Color>(samples.Length, Allocator.TempJob);
    Texture2D fbmTexture2D = new Texture2D(width,height,TextureFormat.RGB24, false);
      
    var job = new SetColorsJob(){samples = samples, colors = colors}
      .Schedule(samples.Length, 64);
    job.Complete();
    
    
    fbmTexture2D.SetPixels(colors.ToArray());
    fbmTexture2D.Apply(false, false);

    samples.Dispose();
    colors.Dispose();
    
    return fbmTexture2D;
  }

  static int CoordinatesToIndex(int x, int y, int width) {
    return width * y + x;
  }

  static void IndexToCoordinates(out int x, out int y, int index, int width) {
    x = index % width;
    y = index / width;
  }
  
  static unsafe NativeArray<Color> GetNativeArrayColor(Color[] colorArray)
  {
    NativeArray<Color> colors = new NativeArray<Color>(colorArray.Length, Allocator.TempJob,
      NativeArrayOptions.UninitializedMemory);

    fixed (void* vertexBufferPointer = colorArray)
    {
      UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(colors),
        vertexBufferPointer, colorArray.Length * (long) UnsafeUtility.SizeOf<Color>());
    }
    return colors;
  }
}

[BurstCompile(CompileSynchronously = true)]
internal struct DistortJob : IJobParallelFor {
  [ReadOnly] public NativeArray<float> samples;
  [ReadOnly] public NativeArray<Color> source;
  public NativeArray<Color> destination;
  public int width, height;
  public float distortAmount;
  public void Execute(int i) {
    float2 coords = new float2();
    int x = i % width;
    int y = i / width;
    float noiseVal = samples[i];
    int shift = (int)math.round(noiseVal * distortAmount);
    destination[i] = source[width * math.clamp((y + shift), 0, height - 1)
                            + math.clamp((x + shift), 0, width - 1)];
  }
}



[BurstCompile(CompileSynchronously = true)]
internal struct SetColorsJob : IJobParallelFor {
  [ReadOnly]public NativeArray<float> samples;
  [WriteOnly]public NativeArray<Color> colors;
  public void Execute(int i) {
    float sample = samples[i];
    colors[i] = new Color(sample,sample,sample);
  }
}

[BurstCompile(CompileSynchronously = true)]
internal struct PerlinJob : IJobParallelFor {
  [WriteOnly] public NativeArray<float> samples;
  public int width, height;
  public float scale, origin;
  public void Execute(int i) {
    float2 coords = new float2();
    int x = i % width;
    int y = i / width;
    coords.x = origin + (float)x / (float)width * scale;
    coords.y = origin + (float)y / (float)height * scale;
    
    samples[i] = Unity.Mathematics.noise.cnoise(coords);
  }
}

[BurstCompile(CompileSynchronously = true)]
internal struct FbmJob : IJobParallelFor {
  public NativeArray<float> samples;
  public int width, height, octaves;
  public float scale, origin, persistance, lacunarity;
  public void Execute(int i) {
    int x = i % width;
    int y = i / width;
    
    float frequency = 1;
    float amplitude = 1;
    float2 coords = new float2();
    for (int j = 0; j < octaves; j++) {
      
      coords.x = origin + (float)x / (float)width * scale * frequency;
      coords.y = origin + (float)y / (float)height * scale * frequency;
      samples[i] += Unity.Mathematics.noise.cnoise(coords)*amplitude;

      amplitude *= persistance;
      frequency *= lacunarity;
    }
  }
}
