using System;
using System.IO;
using System.Linq;
using CircusPrototype;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class CircusPolishSetup
{
    const string Root="Assets/Prototype/";
    static void Center(RectTransform r,Vector2 pos,Vector2 size) { r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size; }
    public static string FixUi()
    {
        var hud=UnityEngine.Object.FindFirstObjectByType<CircusHud>();var so=new SerializedObject(hud);
        var settings=(so.FindProperty("settings").objectReferenceValue as GameObject).transform;
        var slider=so.FindProperty("volumeSlider").objectReferenceValue as Slider;
        Center(slider.GetComponent<RectTransform>(),new Vector2(100,74),new Vector2(600,60));
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/이미지/자산 8.png");
        var white=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Materials/UI White.png");
        var frame=slider.GetComponent<Image>();if(!frame)frame=slider.gameObject.AddComponent<Image>();frame.sprite=sprite;frame.color=new Color(.8f,.63f,.3f);frame.raycastTarget=true;
        var label=settings.Find("전체 음량").GetComponent<TextMeshProUGUI>();Center(label.rectTransform,new Vector2(-330,74),new Vector2(220,50));label.fontSize=27;label.color=new Color(.95f,.88f,.72f);label.transform.SetAsLastSibling();
        var track=slider.transform.Find("Track").GetComponent<Image>();Center(track.rectTransform,Vector2.zero,new Vector2(500,10));track.sprite=white;track.color=new Color(.16f,.12f,.13f);track.raycastTarget=false;
        var fill=slider.fillRect;var handle=slider.handleRect;
        RectTransform area=slider.transform.Find("Fill Area") as RectTransform;
        if(!area) { area=new GameObject("Fill Area",typeof(RectTransform)).GetComponent<RectTransform>();area.SetParent(slider.transform,false); }
        Center(area,Vector2.zero,new Vector2(500,10));fill.SetParent(area,false);fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;
        var fillImage=fill.GetComponent<Image>();fillImage.sprite=white;fillImage.type=Image.Type.Simple;fillImage.color=new Color(.82f,.64f,.32f);fillImage.raycastTarget=false;
        RectTransform handleArea=slider.transform.Find("Handle Area") as RectTransform;
        if(!handleArea) { handleArea=new GameObject("Handle Area",typeof(RectTransform)).GetComponent<RectTransform>();handleArea.SetParent(slider.transform,false); }
        Center(handleArea,Vector2.zero,new Vector2(500,30));handle.SetParent(handleArea,false);handle.anchorMin=handle.anchorMax=new Vector2(.5f,.5f);handle.pivot=new Vector2(.5f,.5f);handle.sizeDelta=new Vector2(18,26);handle.anchoredPosition=Vector2.zero;
        var handleImage=handle.GetComponent<Image>();handleImage.sprite=white;handleImage.color=new Color(.97f,.88f,.62f);handleImage.raycastTarget=true;slider.targetGraphic=handleImage;
        slider.fillRect=fill;slider.handleRect=handle;float original=slider.value;slider.SetValueWithoutNotify(0);slider.SetValueWithoutNotify(original);
        var value=slider.transform.Find("Volume Value")?.GetComponent<TextMeshProUGUI>();
        if(!value) { value=UnityEngine.Object.Instantiate(label,slider.transform);value.name="Volume Value"; }
        Center(value.rectTransform,new Vector2(0,-47),new Vector2(200,35));value.fontSize=21;value.text="70%";value.raycastTarget=false;
        so.FindProperty("volumeValueText").objectReferenceValue=value;
        foreach(var text in settings.GetComponentsInChildren<TextMeshProUGUI>(true)) if(text.text.Contains("3초 준비"))text.text="전체 음량과 화면 모드를 설정합니다.\n슬라이더를 끌어 음량을 변경하세요.";
        var panel=(so.FindProperty("bossPanel").objectReferenceValue as GameObject).GetComponent<RectTransform>();Center(panel,new Vector2(0,305),new Vector2(850,110));
        var bossLabel=so.FindProperty("bossHealthText").objectReferenceValue as TextMeshProUGUI;Center(bossLabel.rectTransform,new Vector2(0,14),new Vector2(690,30));bossLabel.fontSize=23;
        var bossFill=so.FindProperty("bossHealthFill").objectReferenceValue as Image;Center(bossFill.rectTransform,new Vector2(0,-18),new Vector2(696,16));bossFill.sprite=white;bossFill.color=new Color(.08f,.68f,.48f);
        var bossTrack=panel.Find("HP Track").GetComponent<Image>();Center(bossTrack.rectTransform,new Vector2(0,-18),new Vector2(700,20));bossTrack.sprite=white;bossTrack.color=new Color(.18f,.12f,.16f);
        so.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();return "Slider and boss bar insets fixed";
    }
    static float[] Samples(string path,int rate,float seconds)
    {
        var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(!clip)throw new Exception("Missing source: "+path);
        clip.LoadAudioData();var data=new float[clip.samples*clip.channels];if(!clip.GetData(data,0))throw new Exception("Cannot decode "+path);
        int n=Math.Min(Mathf.RoundToInt(seconds*rate),Mathf.FloorToInt(clip.samples*(float)rate/clip.frequency));var result=new float[n];
        for(int i=0;i<n;i++){int src=Math.Min(clip.samples-1,Mathf.FloorToInt(i*(float)clip.frequency/rate));for(int c=0;c<clip.channels;c++)result[i]+=data[src*clip.channels+c]/clip.channels;}
        return result;
    }
    public static string PrepareSounds()
    {
        const int rate=44100;string folder=Root+"Audio/Polished";Directory.CreateDirectory(folder);
        string free=Root+"Audio/Free/";
        var map=new[]{"KenneyInterface/glass_002","KenneyInterface/confirmation_001","JaggedStoneMagic/magical_2","JaggedStoneMagic/magical_7","KenneyRPG/knifeSlice","JaggedStoneMagic/magical_1","KenneyRPG/chop","KenneyRPG/creak1","KenneyRPG/knifeSlice2","KenneyRPG/dropLeather","JaggedStoneMagic/magical_4","KenneyRPG/chop","JaggedStoneMagic/magical_5","KenneyRPG/metalPot1","JaggedStoneMagic/magical_6","","KenneyInterface/glass_001","KenneyInterface/bong_001","JaggedStoneMagic/magical_3","KenneyInterface/click_001"};
        float[] lengths={.2f,.5f,1,1.8f,.35f,.55f,.22f,.8f,.2f,.5f,1.5f,.6f,.8f,.3f,1.8f,.65f,.4f,.4f,.55f,.15f};
        var names=Enum.GetNames(typeof(CircusCue));
        for(int i=0;i<map.Length;i++)
        {
            string source=map[i].Length>0 ? free+map[i]+".ogg" : Root+"Audio/Generated/FlameFlight.wav";
            var importer=AssetImporter.GetAtPath(source) as AudioImporter;
            if(importer){var settings=importer.defaultSampleSettings;if(settings.loadType!=AudioClipLoadType.DecompressOnLoad){settings.loadType=AudioClipLoadType.DecompressOnLoad;importer.defaultSampleSettings=settings;importer.SaveAndReimport();}}
            var data=Samples(source,rate,lengths[i]);
            if(names[i]=="PlayerMelee" || names[i]=="FlameImpact" || names[i]=="BossMelee")
            {
                var bell=Samples(Root+"Audio/Generated/"+names[i]+".wav",rate,lengths[i]);
                for(int n=0;n<Math.Min(data.Length,bell.Length);n++)data[n]=data[n]*.8f+bell[n]*.2f;
            }
            float peak=data.Select(Mathf.Abs).DefaultIfEmpty(1).Max();
            using(var writer=new BinaryWriter(File.Create(folder+"/"+names[i]+".wav")))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+data.Length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(data.Length*2);
                for(int n=0;n<data.Length;n++){float fade=Mathf.Min(1,n/(rate*.003f))*Mathf.Min(1,(data.Length-n)/(rate*.035f));writer.Write((short)(Mathf.Clamp(data[n]/Mathf.Max(.001f,peak)*.65f*fade,-1,1)*32767));}
            }
        }
        AssetDatabase.Refresh();return "20 normalized, trimmed sound cues prepared";
    }
    public static string BindSounds()
    {
        var audio=UnityEngine.Object.FindFirstObjectByType<CircusAudio>();var so=new SerializedObject(audio);var clips=so.FindProperty("clips");var names=Enum.GetNames(typeof(CircusCue));
        float[] volumes={.7f,.8f,.8f,.9f,.85f,.6f,.7f,.75f,.55f,.6f,.8f,.9f,.7f,.55f,.85f,.25f,.6f,.6f,.65f,.55f};var gains=so.FindProperty("cueVolumes");gains.arraySize=clips.arraySize=volumes.Length;
        for(int i=0;i<volumes.Length;i++){clips.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/Polished/"+names[i]+".wav");gains.GetArrayElementAtIndex(i).floatValue=volumes[i];}
        so.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(audio.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();return "20 polished cues bound";
    }
}
