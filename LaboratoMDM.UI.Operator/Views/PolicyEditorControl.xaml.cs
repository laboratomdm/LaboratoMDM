using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;
using LaboratoMDM.UI.Operator.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace LaboratoMDM.UI.Operator.Views
{
    public partial class PolicyEditorControl : UserControl
    {
        public PolicyEditorControl()
        {
            InitializeComponent();
        }

        // DependencyProperty для передачи PolicyDetails
        //public static readonly DependencyProperty PolicyProperty =
        //    DependencyProperty.Register(
        //        nameof(Policy),
        //        typeof(PolicyDetails),
        //        typeof(PolicyEditorControl),
        //        new PropertyMetadata(null, OnPolicyChanged));

        //public PolicyDetails Policy
        //{
        //    get => (PolicyDetails)GetValue(PolicyProperty);
        //    set => SetValue(PolicyProperty, value);
        //}

        //private static void OnPolicyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    if (d is PolicyEditorControl control && e.NewValue is PolicyDetails policy)
        //    {
        //        var vm = new PolicyEditorViewModel();
        //        vm.LoadFromPolicyDetails(policy);
        //        control.DataContext = vm;
        //    }
        //}
    }
}
