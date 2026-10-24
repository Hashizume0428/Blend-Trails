using TMPro;
using UnityEngine;

/// <summary>Staggered glyph animation using TMP's original vertices, including mesh rebuilds.</summary>
[RequireComponent(typeof(TMP_Text))]
public sealed class ClearTitleAnimator : MonoBehaviour
{
    private TMP_Text _text;
    private TMP_MeshInfo[] _original;
    private bool _active;
    private float _seconds;
    private void OnEnable()
    {
        _text = GetComponent<TMP_Text>();
        _text.OnPreRenderText += Rebuilt;
    }
    public void Begin()
    {
        _active = true; _seconds = 0;
        _text.ForceMeshUpdate();
    }
    private void Rebuilt(TMP_TextInfo info)
    {
        if (!_active) return;
        _original = info.CopyMeshInfoVertexData();
        Apply(info);
    }
    public void SetTime(float seconds)
    {
        if (!_active || _original == null) return;
        _seconds = seconds;
        Apply(_text.textInfo);
        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }
    private void Apply(TMP_TextInfo info)
    {
        if (_original == null || info.meshInfo.Length != _original.Length) return;
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];
            if (!character.isVisible) continue;
            int material = character.materialReferenceIndex, vertex = character.vertexIndex;
            if (vertex + 3 >= _original[material].vertices.Length) continue;
            Vector3[] source = _original[material].vertices, output = info.meshInfo[material].vertices;
            Vector3 center = (source[vertex] + source[vertex + 2]) * .5f;
            float p = ClearMotionMath.Progress(_seconds, 1.07f + i * .022f, 1.45f + i * .022f);
            float pop = ClearMotionMath.Back(p), scale = Mathf.LerpUnclamped(.55f, 1, pop);
            Quaternion rotation = Quaternion.Euler(0, 0, Mathf.Lerp(i % 2 == 0 ? -5.7f : 5.7f, 0, p));
            Vector3 offset = Vector3.up * Mathf.LerpUnclamped(-65, 0, pop);
            for (int j = 0; j < 4; j++)
            {
                output[vertex + j] = center + offset + rotation * ((source[vertex + j] - center) * scale);
                Color32 tint = _original[material].colors32[vertex + j];
                tint.a = (byte)(tint.a * ClearMotionMath.Out(p));
                info.meshInfo[material].colors32[vertex + j] = tint;
            }
        }
    }
    public void Restore()
    {
        if (_active && _text != null && _original != null)
        {
            TMP_TextInfo info = _text.textInfo;
            for (int i = 0; i < Mathf.Min(info.meshInfo.Length, _original.Length); i++)
            {
                int count = Mathf.Min(info.meshInfo[i].vertices.Length, _original[i].vertices.Length);
                System.Array.Copy(_original[i].vertices, info.meshInfo[i].vertices, count);
                System.Array.Copy(_original[i].colors32, info.meshInfo[i].colors32, count);
            }
            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }
        _active = false; _original = null;
    }
    private void OnDisable()
    {
        Restore();
        if (_text != null) _text.OnPreRenderText -= Rebuilt;
    }
}
