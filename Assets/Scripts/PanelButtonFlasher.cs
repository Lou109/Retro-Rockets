using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates a "retro sci-fi control panel" effect by showing only two color materials
/// at a time on the LunarModule button renderers. All other button materials become ButtonUnlit.
///
/// Setup:
/// 1) Add this component to LunarModule (or the GameObject that owns the button renderers).
/// 2) Assign ButtonUnlit material.
/// 3) Assign the color materials (Red/Blue/Yellow/Orange/Green/Pink).
/// 4) Leave Auto Collect enabled to auto-find button renderers by material usage.
/// </summary>
public class PanelButtonFlasher : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] Material buttonUnlitMaterial;

    [Tooltip("All available 'on' color materials. This script will randomly choose 2 at a time.")]
    [SerializeField] Material[] colorMaterials;

    [Header("Targets")]
    [Tooltip("If set, only these renderers are treated as panel buttons.")]
    [SerializeField] Renderer[] buttonRenderers;

    [Tooltip("If true and Button Renderers is empty, button renderers will be auto-collected from children by material usage.")]
    [SerializeField] bool autoCollectButtonRenderers = true;

    [Header("Flash")]
    [Tooltip("Seconds between material swaps. Try 0.05–0.25 for a good blinking panel vibe.")]
    [SerializeField] float flashIntervalSeconds = 0.12f;

    readonly List<Renderer> runtimeButtons = new();
    readonly Dictionary<Renderer, Material[]> originalMaterialsByRenderer = new();
    readonly HashSet<Material> colorSet = new();
    readonly List<Material> colorsUsedByButtons = new();

    float timer;

    void Awake()
    {
        RefreshColorSet();
        CollectButtonRenderers();
        CaptureOriginalMaterials();
        CacheColorsUsedByButtons();

        // Initialize to a valid state immediately.
        ApplyNewFlashSelection();
    }

    void OnValidate()
    {
        if (flashIntervalSeconds < 0f)
        {
            flashIntervalSeconds = 0f;
        }
    }

    void Update()
    {
        if (runtimeButtons.Count == 0)
        {
            return;
        }

        if (flashIntervalSeconds <= 0f)
        {
            // If interval is 0, do a per-frame flash (can be a lot; use with care).
            ApplyNewFlashSelection();
            return;
        }

        timer += Time.deltaTime;
        if (timer >= flashIntervalSeconds)
        {
            timer = 0f;
            ApplyNewFlashSelection();
        }
    }

    void RefreshColorSet()
    {
        colorSet.Clear();
        if (colorMaterials == null)
        {
            return;
        }

        for (int i = 0; i < colorMaterials.Length; i++)
        {
            var m = colorMaterials[i];
            if (m != null)
            {
                colorSet.Add(m);
            }
        }
    }

    void CollectButtonRenderers()
    {
        runtimeButtons.Clear();

        if (buttonRenderers != null && buttonRenderers.Length > 0)
        {
            for (int i = 0; i < buttonRenderers.Length; i++)
            {
                var r = buttonRenderers[i];
                if (r != null)
                {
                    runtimeButtons.Add(r);
                }
            }

            return;
        }

        if (!autoCollectButtonRenderers)
        {
            return;
        }

        // Auto-collect: find renderers that use any of the specified button materials.
        // This avoids grabbing unrelated renderers (body/engine/etc.).
        var all = GetComponentsInChildren<Renderer>(includeInactive: true);
        for (int i = 0; i < all.Length; i++)
        {
            var r = all[i];
            if (r == null)
            {
                continue;
            }

            var mats = r.sharedMaterials;
            bool usesButtonMaterial = false;
            for (int j = 0; j < mats.Length; j++)
            {
                var m = mats[j];
                if (m == null)
                {
                    continue;
                }

                if (m == buttonUnlitMaterial || colorSet.Contains(m))
                {
                    usesButtonMaterial = true;
                    break;
                }
            }

            if (usesButtonMaterial)
            {
                runtimeButtons.Add(r);
            }
        }
    }

    void CaptureOriginalMaterials()
    {
        originalMaterialsByRenderer.Clear();

        for (int i = 0; i < runtimeButtons.Count; i++)
        {
            var r = runtimeButtons[i];
            if (r == null)
            {
                continue;
            }

            originalMaterialsByRenderer[r] = r.sharedMaterials;
        }
    }

    void CacheColorsUsedByButtons()
    {
        colorsUsedByButtons.Clear();

        foreach (var kvp in originalMaterialsByRenderer)
        {
            var mats = kvp.Value;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m != null && colorSet.Contains(m) && !colorsUsedByButtons.Contains(m))
                {
                    colorsUsedByButtons.Add(m);
                }
            }
        }
    }

    void ApplyNewFlashSelection()
    {
        if (buttonUnlitMaterial == null)
        {
            Debug.LogWarning($"{nameof(PanelButtonFlasher)}: ButtonUnlit material is not assigned.", this);
            return;
        }

        var pool = colorsUsedByButtons.Count > 0 ? colorsUsedByButtons : GetFallbackColorPool();
        if (pool.Count == 0)
        {
            Debug.LogWarning($"{nameof(PanelButtonFlasher)}: No color materials assigned/available.", this);
            return;
        }

        PickTwoDistinct(pool, out var keepA, out var keepB);

        // Normal mode: buttons already have different color materials assigned in the mesh.
        // We keep the two chosen colors and force every other button-color to ButtonUnlit.
        if (colorsUsedByButtons.Count > 0)
        {
            foreach (var kvp in originalMaterialsByRenderer)
            {
                var renderer = kvp.Key;
                if (renderer == null)
                {
                    continue;
                }

                var original = kvp.Value;
                var newMats = new Material[original.Length];
                for (int i = 0; i < original.Length; i++)
                {
                    var o = original[i];

                    // Only replace the button color materials; keep any non-button materials untouched.
                    if (o != null && colorSet.Contains(o))
                    {
                        newMats[i] = (o == keepA || o == keepB) ? o : buttonUnlitMaterial;
                    }
                    else
                    {
                        newMats[i] = o;
                    }
                }

                renderer.sharedMaterials = newMats;
            }

            return;
        }

        // Fallback mode: if the buttons all start out as ButtonUnlit (or don't currently use
        // the color materials), randomly light up two buttons by setting one slot per renderer.
        if (runtimeButtons.Count == 0)
        {
            return;
        }

        int buttonA = Random.Range(0, runtimeButtons.Count);
        int buttonB;
        if (runtimeButtons.Count == 1)
        {
            buttonB = buttonA;
        }
        else
        {
            do
            {
                buttonB = Random.Range(0, runtimeButtons.Count);
            } while (buttonB == buttonA);
        }

        for (int i = 0; i < runtimeButtons.Count; i++)
        {
            var renderer = runtimeButtons[i];
            if (renderer == null || !originalMaterialsByRenderer.TryGetValue(renderer, out var original))
            {
                continue;
            }

            var newMats = new Material[original.Length];
            for (int j = 0; j < original.Length; j++)
            {
                var o = original[j];
                if (o == buttonUnlitMaterial || (o != null && colorSet.Contains(o)))
                {
                    newMats[j] = buttonUnlitMaterial;
                }
                else
                {
                    newMats[j] = o;
                }
            }

            int slot = GetFirstButtonSlotIndex(original);
            if (slot >= 0)
            {
                if (i == buttonA)
                {
                    newMats[slot] = keepA;
                }
                else if (i == buttonB)
                {
                    newMats[slot] = keepB;
                }
            }

            renderer.sharedMaterials = newMats;
        }
    }

    int GetFirstButtonSlotIndex(Material[] original)
    {
        for (int i = 0; i < original.Length; i++)
        {
            var o = original[i];
            if (o == buttonUnlitMaterial || (o != null && colorSet.Contains(o)))
            {
                return i;
            }
        }

        return -1;
    }

    List<Material> GetFallbackColorPool()
    {
        // If we couldn't infer from the mesh (e.g. buttons start unlit), use the provided list.
        var pool = new List<Material>();
        if (colorMaterials == null)
        {
            return pool;
        }

        for (int i = 0; i < colorMaterials.Length; i++)
        {
            var m = colorMaterials[i];
            if (m != null && !pool.Contains(m))
            {
                pool.Add(m);
            }
        }

        return pool;
    }

    static void PickTwoDistinct(List<Material> pool, out Material a, out Material b)
    {
        if (pool.Count == 1)
        {
            a = pool[0];
            b = pool[0];
            return;
        }

        int iA = Random.Range(0, pool.Count);
        int iB;
        do
        {
            iB = Random.Range(0, pool.Count);
        } while (iB == iA);

        a = pool[iA];
        b = pool[iB];
    }
}
