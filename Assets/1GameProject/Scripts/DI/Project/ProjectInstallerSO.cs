using _1GameProject.Scripts.Audio;
using _1GameProject.Scripts.GameData;
using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "SOProjectInstaller", menuName = "Installers/SOProjectInstaller")]
public class ProjectInstallerSO : ScriptableObjectInstaller<ProjectInstallerSO>
{
    public SoundLibrarySO SoundLibrary;
    [SerializeField] private CampaignRouteSO MainCampaign;
    
    
    public override void InstallBindings()
    {
        Container.BindInstance(SoundLibrary).IfNotBound();
        Container.BindInstance(MainCampaign).IfNotBound();
    }
}