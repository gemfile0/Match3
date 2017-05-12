using System;
using DG.Tweening;
using UnityEngine;

public class GemView: BaseView<GemModel, GemController<GemModel>>
{
    SpriteRenderer spriteRenderer;
    TextMesh idText;
    TextMesh markerIdText;
    bool isDebugging = true;
    MaterialPropertyBlock mpb;
    Sequence squash;
    
    void Awake()
    {
        idText = ResourceCache.Instantiate(Literals.ID, transform).GetComponent<TextMesh>();
        markerIdText = ResourceCache.Instantiate(Literals.MarkerID, transform).GetComponent<TextMesh>();
        markerIdText.gameObject.SetActive(false);
        mpb = new MaterialPropertyBlock();
        spriteRenderer = GetComponent<SpriteRenderer>();
        squash = DOTween.Sequence();
        squash.Append(transform.DOScale(new Vector3(1.08f, 0.92f, 1), 0.12f));
        squash.Append(transform.DOScale(new Vector3(1, 1, 1), 0.68f).SetEase(Ease.OutElastic));
        squash.Pause();
        squash.SetAutoKill(false);

#if DISABLE_DEBUG
        isDebugging = false;
#endif
    }
    
    public Position Position 
    { 
        get { return Model.Position; } 
    }

    public Int64 PreservedFromMatch
    {
        get { return Model.preservedFromMatch; }
    }

    public override void UpdateModel(GemModel gemModel) 
    {
        base.UpdateModel(gemModel);

        idText.text = gemModel.id.ToString();
    }
    
    public void Highlight() 
    {
        spriteRenderer.GetPropertyBlock(mpb);

        if (!isDebugging) { return; }
        DOTween.To(
            GetFlashAmount,
            SetFlashAmount, 
            .2f, 
            .395f
        ).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    float GetFlashAmount()
    {
        return mpb.GetFloat("_FlashAmount");
    }

    void SetFlashAmount(float value)
    {
        mpb.SetFloat("_FlashAmount", value);
        spriteRenderer.SetPropertyBlock(mpb);
    }

    public void Squash() 
    {
        squash.Restart();

        if (!isDebugging) { return; }
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_FlashAmount", 0.0f);
        spriteRenderer.SetPropertyBlock(mpb);
    }

    public void Reveal() 
    {
        gameObject.SetActive(true);

        var color = spriteRenderer.color;
        spriteRenderer.color = new Color(color.r, color.g, color.b, 1f);

        if (!isDebugging) { return; }
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_FlashAmount", 0.4f);
        spriteRenderer.SetPropertyBlock(mpb);
    }

    public void Hide()
    {
        var color = spriteRenderer.color;
        spriteRenderer.color = new Color(color.r, color.g, color.b, 0.1f);
    }

    public void SetBlock(Int64 markerID) 
    {
        if (!isDebugging) { return; }

        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_FlashAmount", 0.4f);
        mpb.SetColor("_FlashColor", new Color32(255, 0, 0, 1));
        spriteRenderer.SetPropertyBlock(mpb);

        markerIdText.text = markerID.ToString();
        markerIdText.gameObject.SetActive(true);
    }

    public void SetActive(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public override void ReturnToPool()
    {   
        markerIdText.gameObject.SetActive(false);
        base.ReturnToPool();
        
        if (!isDebugging || spriteRenderer == null) { return; }

        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_FlashAmount", 0.0f);
        spriteRenderer.SetPropertyBlock(mpb);
    }
}
