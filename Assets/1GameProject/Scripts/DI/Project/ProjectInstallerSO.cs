using _1GameProject.Scripts.Audio;
using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "SOProjectInstaller", menuName = "Installers/SOProjectInstaller")]
public class ProjectInstallerSO : ScriptableObjectInstaller<ProjectInstallerSO>
{
    public SoundLibrarySO SoundLibrary;
    
    public override void InstallBindings()
    {
        Container.BindInstance(SoundLibrary).IfNotBound();
    }
}