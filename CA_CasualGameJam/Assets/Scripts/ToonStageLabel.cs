using TMPro;
using UnityEngine;

/// <summary>Formats the existing navigation labels without changing their saved progress or API.</summary>
[ExecuteAlways, RequireComponent(typeof(TMP_Text))]
public sealed class ToonStageLabel : MonoBehaviour, ITextPreprocessor
{
    private TMP_Text _text;
    private void OnEnable() => Configure();
    public void Configure()
    {
        _text = GetComponent<TMP_Text>();
        _text.textPreprocessor = this;
        if (!_text.enableAutoSizing) _text.fontSizeMax = _text.fontSize;
        _text.fontSizeMin = 32;
        _text.enableAutoSizing = true;
        _text.SetAllDirty();
    }
    private void OnDisable()
    {
        if (_text != null && ReferenceEquals(_text.textPreprocessor, this)) _text.textPreprocessor = null;
    }
    public string PreprocessText(string value) => value.Replace("-", "").Replace("Stage", "STAGE ");
}
