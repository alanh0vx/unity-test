using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace VXRL2.Editor {
// Local, project-scoped build automation. No sockets, remote commands, or arbitrary evaluation.
[InitializeOnLoad]
public static class LocalBuildJobs {
 static readonly string Request=Path.GetFullPath("Validation/editor-job.txt");
 static LocalBuildJobs(){EditorApplication.update+=Poll;}
 static void Poll(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
  string job=File.ReadAllText(Request).Trim();File.Delete(Request);
  try {
   if(job=="combat"){if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;File.WriteAllText(Request,"combat");return;}CombatFeelSetup.Build();}
   else if(job=="interface"){if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;File.WriteAllText(Request,"interface");return;}InterfaceSetup.Build();}
   else if(job=="generate")ProjectBuilder.Generate();
   else if(job=="build")ProjectBuilder.Build();
   else if(job=="webgl"){if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;File.WriteAllText(Request,"webgl");return;}ProjectBuilder.BuildWebGL();}
   else if(job=="play"){EditorSceneManager.OpenScene("Assets/VXRL2/Scenes/Rootbreach.unity");EditorApplication.isPlaying=true;}
   else if(job=="stop")EditorApplication.isPlaying=false;
   else throw new Exception("Unknown local build job");
   File.WriteAllText("Validation/editor-job-result.txt",job+" OK "+DateTime.UtcNow.ToString("O"));
  }catch(Exception ex){Debug.LogException(ex);File.WriteAllText("Validation/editor-job-result.txt",job+" FAILED "+ex);}
 }
}
}
