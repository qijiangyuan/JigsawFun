using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SpriteUtils
{
  /// <summary>
  /// 从给定的 Texture2D 创建 Sprite，默认使用 FullRect 网格类型以避免运行时生成紧密网格（Tight）带来的额外开销，
  /// 尤其在移动端可降低 CPU 负担并减少生成时的延迟。
  /// </summary>
  /// <param name="spriteTexture">源纹理（应包含裁剪区域且可读）。</param>
  /// <param name="x">裁剪矩形左下角 X 像素坐标。</param>
  /// <param name="y">裁剪矩形左下角 Y 像素坐标。</param>
  /// <param name="w">裁剪矩形宽度（像素）。</param>
  /// <param name="h">裁剪矩形高度（像素）。</param>
  /// <param name="pixelsPerUnit">每单位像素数（默认 1.0f）。</param>
  /// <param name="spriteType">Sprite 网格类型，默认 FullRect。</param>
  public static Sprite CreateSpriteFromTexture2D(
    Texture2D spriteTexture,
    int x,
    int y,
    int w,
    int h,
    float pixelsPerUnit = 1.0f,
    SpriteMeshType spriteType = SpriteMeshType.FullRect)
  {
    Sprite newSprite = Sprite.Create(
      spriteTexture,
      new Rect(x, y, w, h),
      new Vector2(0, 0),
      pixelsPerUnit,
      0,
      spriteType);
    return newSprite;
  }

  // 说明：从 Resources 路径加载 Texture2D 资源。
  public static Texture2D LoadTexture(string resourcePath)
  {
    Texture2D tex = Resources.Load<Texture2D>(resourcePath);
    return tex;
  }
}
