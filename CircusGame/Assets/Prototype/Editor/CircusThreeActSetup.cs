using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using CircusPrototype;
public static class CircusThreeActSetup
{
    const string Root="Assets/Prototype/";
    static void Set(Object target,string key,Object value) { var so=new SerializedObject(target); so.FindProperty(key).objectReferenceValue=value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void Number(Object target,string key,float value) { var so=new SerializedObject(target); so.FindProperty(key).floatValue=value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static Material Material(string title,Color color,bool unlit=false)
    {
        string path=Root+"Materials/"+title+".mat";
        var result=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!result) { result=new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(result,path); }
        result.SetColor("_BaseColor",color);
        if(!unlit) { result.EnableKeyword("_EMISSION"); result.SetColor("_EmissionColor",color*2.5f); }
        return result;
    }
    public static string ConfigureCombat()
    {
        var session=Object.FindFirstObjectByType<CircusSession>();
        if(!session || EditorApplication.isPlaying) throw new System.InvalidOperationException("Prototype scene in Edit Mode required");
        string bossPath=Root+"Prefabs/DokkaebiBoss.prefab";
        var boss=PrefabUtility.LoadPrefabContents(Root+"Prefabs/CloseRang.prefab"); boss.name="Dokkaebi Boss";
        var enemy=boss.GetComponent<CircusEnemy>();
        Number(enemy,"health",650);
        if(!boss.GetComponent<CircusBoss>()) boss.AddComponent<CircusBoss>();
        var model=new SerializedObject(enemy).FindProperty("model").objectReferenceValue as Transform;
        var art=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/모델링/근거리.obj");
        if(art)
        {
            Object.DestroyImmediate(model.gameObject);
            var visual=Object.Instantiate(art,boss.transform); visual.name="Artist Boss Model";
            var renderers=visual.GetComponentsInChildren<Renderer>(); var bounds=renderers[0].bounds;
            foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            visual.transform.localScale*=3.1f/bounds.size.y;
            bounds=renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            visual.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)-boss.transform.position;
            model=visual.transform; Set(enemy,"model",model);
            var palette=Material("Boss Artist Palette",Color.white);
            palette.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/모델링/근거리.png"));
            palette.SetColor("_EmissionColor",Color.black);
            foreach(var renderer in renderers) renderer.sharedMaterial=palette;
            foreach(var collider in visual.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
        }
        else model.localScale*=1.7f;
        var controller=boss.GetComponent<CharacterController>(); controller.height=3.05f; controller.radius=.7f; controller.center=Vector3.up*1.525f;
        var bar=boss.GetComponentInChildren<Canvas>(); if(bar) bar.gameObject.SetActive(false);
        Set(enemy,"projectile",AssetDatabase.LoadAssetAtPath<CircusProjectile>(Root+"Prefabs/RangProjectile.prefab"));
        var saved=PrefabUtility.SaveAsPrefabAsset(boss,bossPath); PrefabUtility.UnloadPrefabContents(boss);
        Set(session,"bossPrefab",saved.GetComponent<CircusEnemy>());
        foreach(bool player in new[]{false,true})
        {
            string path=Root+"Prefabs/"+(player ? "BellSound" : "RangProjectile")+".prefab";
            var shot=PrefabUtility.LoadPrefabContents(path);
            var color=player ? new Color(1,.64f,.1f) : new Color(.05f,1,.75f);
            var material=Material(player ? "Muin Flame" : "Dokkaebi Flame",color);
            shot.transform.localScale=Vector3.one*(player ? .44f : .56f);
            foreach(var renderer in shot.GetComponentsInChildren<MeshRenderer>()) { renderer.sharedMaterial=material; renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; }
            var trail=shot.GetComponent<TrailRenderer>(); if(!trail) trail=shot.AddComponent<TrailRenderer>();
            trail.time=.24f; trail.startWidth=player ? .22f : .32f; trail.endWidth=.015f; trail.minVertexDistance=.04f;
            trail.sharedMaterial=Material(player ? "Muin Flame Trail" : "Dokkaebi Flame Trail",color,true);
            trail.startColor=Color.white; trail.endColor=new Color(color.r,color.g,color.b,0);
            var light=shot.GetComponent<Light>(); if(!light) light=shot.AddComponent<Light>();
            light.type=LightType.Point; light.color=color; light.range=2.3f; light.intensity=1.4f; light.shadows=LightShadows.None;
            Number(shot.GetComponent<CircusProjectile>(),"hitRadius",player ? .22f : .28f);
            var so=new SerializedObject(shot.GetComponent<CircusProjectile>()); so.FindProperty("flameColor").colorValue=color; so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(shot,path); PrefabUtility.UnloadPrefabContents(shot);
        }
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene); EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
        return "Boss prefab, 650 HP, projectile visuals configured";
    }
    public static string ConfigureHud()
    {
        var hud=Object.FindFirstObjectByType<CircusHud>();
        var so=new SerializedObject(hud);
        var gameplay=(so.FindProperty("gameplay").objectReferenceValue as GameObject).transform;
        var message=so.FindProperty("message").objectReferenceValue as TextMeshProUGUI;
        message.rectTransform.anchoredPosition=new Vector2(0,185);
        var text=so.FindProperty("countdownText").objectReferenceValue as TextMeshProUGUI;
        if(!text) { text=Object.Instantiate(message,gameplay); text.name="Countdown"; }
        var frameSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/이미지/자산 8.png");
        var maskSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/이미지/자산 5.png");
        var frame=so.FindProperty("countdownPanel").objectReferenceValue as GameObject;
        if(!frame) {
            var existing=gameplay.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t=>t.name=="Artist Countdown Frame");
            frame=existing ? existing.gameObject : new GameObject("Artist Countdown Frame",typeof(RectTransform),typeof(UnityEngine.UI.Image));
        }
        frame.transform.SetParent(gameplay,false);
        frame.transform.localScale=Vector3.one;
        var frameRect=frame.GetComponent<RectTransform>(); frameRect.anchorMin=frameRect.anchorMax=frameRect.pivot=new Vector2(.5f,.5f);
        frameRect.anchoredPosition=Vector2.zero; frameRect.sizeDelta=new Vector2(680,155);
        var background=frame.GetComponent<UnityEngine.UI.Image>(); background.sprite=frameSprite; background.raycastTarget=false; background.color=Color.white;
        text.transform.SetParent(frame.transform,false); text.transform.localScale=Vector3.one;
        text.rectTransform.anchorMin=text.rectTransform.anchorMax=text.rectTransform.pivot=new Vector2(.5f,.5f);
        text.rectTransform.anchoredPosition=Vector2.zero; text.rectTransform.sizeDelta=new Vector2(620,125);
        text.text="3"; text.fontSize=88; text.fontStyle=FontStyles.Normal;
        var textMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Countdown Type.mat");
        if(!textMaterial) { textMaterial=new Material(text.font.material); AssetDatabase.CreateAsset(textMaterial,Root+"Materials/Countdown Type.mat"); }
        textMaterial.SetFloat("_WeightNormal",0); textMaterial.SetFloat("_WeightBold",0);
        text.fontSharedMaterial=textMaterial; text.UpdateMeshPadding(); text.color=new Color(.15f,.06f,.08f); text.raycastTarget=false;
        Set(hud,"countdownText",text); Set(hud,"countdownPanel",frame);
        if(maskSprite)
        {
            var mask=text.transform.Find("Artist Countdown Mask");
            if(!mask) { var go=new GameObject("Artist Countdown Mask",typeof(RectTransform),typeof(UnityEngine.UI.Image)); go.transform.SetParent(text.transform,false); mask=go.transform; }
            var maskRect=mask.GetComponent<RectTransform>(); maskRect.sizeDelta=new Vector2(130,150); maskRect.anchoredPosition=new Vector2(0,165);
            var image=mask.GetComponent<UnityEngine.UI.Image>(); image.sprite=maskSprite; image.preserveAspect=true;
            image.color=new Color(.85f,.69f,.37f); image.raycastTarget=false;
        }
        text.gameObject.SetActive(false); frame.SetActive(false);
        var panel=gameplay.Find("Boss Health");
        if(!panel) { panel=Object.Instantiate(gameplay.Find("HP Panel"),gameplay); panel.name="Boss Health"; }
        var rect=panel.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=new Vector2(0,310); rect.sizeDelta=new Vector2(850,92);
        foreach(var label in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if(label.text.Contains("100 / 100") || label.name=="Boss HP Label")
            {
                label.gameObject.SetActive(true); label.name="Boss HP Label"; label.rectTransform.anchoredPosition=new Vector2(0,23);
                label.rectTransform.sizeDelta=new Vector2(790,38); label.fontSize=25; label.text="도깨비 대장 650 / 650"; Set(hud,"bossHealthText",label);
            }
            else label.gameObject.SetActive(false);
        }
        foreach(var image in panel.GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            image.raycastTarget=false;
            if(image.name=="HP Track" || image.name=="HP Fill") { image.rectTransform.anchoredPosition=new Vector2(0,-20); image.rectTransform.sizeDelta=new Vector2(image.name=="HP Track" ? 790 : 786,20); }
            if(image.name=="HP Fill") { image.color=new Color(.15f,.9f,.7f); Set(hud,"bossHealthFill",image); }
        }
        var bossFrame=panel.GetComponent<UnityEngine.UI.Image>();
        if(frameSprite) { bossFrame.sprite=frameSprite; bossFrame.type=UnityEngine.UI.Image.Type.Simple; bossFrame.color=Color.white; }
        foreach(var label in panel.GetComponentsInChildren<TextMeshProUGUI>(true)) label.color=new Color(.15f,.06f,.08f);
        Set(hud,"bossPanel",panel.gameObject); panel.gameObject.SetActive(false);
        foreach(var label in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(label.text.Contains("두 개의 막")) label.text="세 개의 막, 마지막 도깨비";
            if(label.text.Contains("두 막을 모두")) label.text="3초 준비 후 공연이 시작됩니다.\n3막의 도깨비 대장을 쓰러뜨리세요.";
            if(label.text.Contains("근접 랑은 추적")) label.text="붉은 예고는 회피 · 금색 부채꼴은 근접 범위";
            if(label.text.Contains("좌클릭") && !label.text.Contains("W A S D")) { label.fontSize=21; label.text="WASD 이동 · SPACE 점프 · V 시점 · 좌클릭 음파 · 우클릭 근접 · ESC 정지"; }
            EditorUtility.SetDirty(label);
        }
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene); EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
        return "Reused artist frame 8, mask 5, team fonts and existing result screen";
    }
    public static string ConfigureAudio()
    {
        var session=Object.FindFirstObjectByType<CircusSession>();
        var audio=session.GetComponent<CircusAudio>(); if(!audio) audio=session.gameObject.AddComponent<CircusAudio>();
        var so=new SerializedObject(audio); var clips=so.FindProperty("clips");
        var names=System.Enum.GetNames(typeof(CircusCue)); clips.arraySize=(int)CircusCue.UIClick;
        for(int i=0;i<clips.arraySize;i++) clips.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/Generated/"+names[i]+".wav");
        var gains=so.FindProperty("cueVolumes"); gains.arraySize=clips.arraySize;
        for(int i=0;i<gains.arraySize;i++) gains.GetArrayElementAtIndex(i).floatValue=1;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene); EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
        return "Original generated audio bank restored: "+clips.arraySize;
    }
}
