using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilesSorting
{
  private List<SpriteRenderer> mSortIndices = new List<SpriteRenderer>();

  public TilesSorting()
  {
  }

  private void Prune()
  {
    for (int i = mSortIndices.Count - 1; i >= 0; i--)
    {
      if (mSortIndices[i] == null)
      {
        mSortIndices.RemoveAt(i);
      }
    }
  }

  public void Clear()
  {
    mSortIndices.Clear();
  }

  public void Add(SpriteRenderer renderer)
  {
    if (renderer == null) return;
    Prune();
    // 避免重复添加
    if (!mSortIndices.Contains(renderer))
    {
      mSortIndices.Add(renderer);
    }
    SetRenderOrder(renderer, mSortIndices.Count);
  }

  public void Remove(SpriteRenderer renderer)
  {
    Prune();
    if (renderer != null)
    {
      mSortIndices.Remove(renderer);
    }
    for(int i = 0; i < mSortIndices.Count; i++)
    {
      SetRenderOrder(mSortIndices[i], i + 1);
    }
  }

  public void BringToTop(SpriteRenderer renderer)
  {
    if (renderer == null) return;
    Prune();
    mSortIndices.Remove(renderer);
    mSortIndices.Add(renderer);
    SetRenderOrder(renderer, mSortIndices.Count);
  }

  private void SetRenderOrder(SpriteRenderer renderer, int index)
  {
    if (renderer == null) return;
    renderer.sortingOrder = index;
    Vector3 p = renderer.transform.position;
    p.z = -index / 10.0f;
    renderer.transform.position = p;
  }
}
