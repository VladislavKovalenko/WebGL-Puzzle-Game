using System;
using Cysharp.Threading.Tasks;

namespace _1GameProject.Scripts.Bootstrap
{
    public interface IBootLoadable
    {
        string LoadingLabel { get; }
        
        bool IsReady { get; }
        
        void Initialize();
        
    }
}