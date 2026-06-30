// Designed by KINEMATION, 2025.

using KINEMATION.KAnimationCore.Editor.Widgets;
using KINEMATION.TacticalShooterPack.Scripts.Weapon;
using UnityEditor;

namespace KINEMATION.TacticalShooterPack.Scripts.Editor
{
    [CustomEditor(typeof(TacticalWeaponSettings)), CanEditMultipleObjects]
    public class TacticalWeaponSettingsEditor : UnityEditor.Editor
    {
        private TabInspectorWidget _tabInspectorWidget;

        private void OnEnable()
        {
            _tabInspectorWidget = new TabInspectorWidget(serializedObject);
            _tabInspectorWidget.Init();
        }

        public override void OnInspectorGUI()
        {
            _tabInspectorWidget.OnGUI();
        }
    }
}