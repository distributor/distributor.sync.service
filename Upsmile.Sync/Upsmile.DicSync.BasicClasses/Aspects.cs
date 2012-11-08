﻿using System;
using PostSharp.Aspects.Advices;
using System.ComponentModel;
using PostSharp.Aspects;
using PostSharp.Extensibility;
using PostSharp.Reflection;

namespace Upsmile.Sync.BasicClasses
{
    /// <summary>
    /// Aspect that, when apply on a class, fully implements the interface 
    /// <see cref="INotifyPropertyChanged"/> into that class, and overrides all properties to
    /// that they raise the event <see cref="INotifyPropertyChanged.PropertyChanged"/>.
    /// </summary>
    [Serializable]
    [IntroduceInterface(typeof(INotifyPropertyChanged),
                         OverrideAction = InterfaceOverrideAction.Ignore)]
    [MulticastAttributeUsage(MulticastTargets.Class,
                              Inheritance = MulticastInheritance.Strict)]
    public sealed class NotifyPropertyChangedAttribute : InstanceLevelAspect,
                                                         INotifyPropertyChanged
    {

        /// <summary>
        /// Field bound at runtime to a delegate of the method <c>OnPropertyChanged</c>.
        /// </summary>
        [ImportMember("OnPropertyChanged", IsRequired = false)]
        public Action<string> OnPropertyChangedMethod;

        /// <summary>
        /// Method introduced in the target type (unless it is already present);
        /// raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        [IntroduceMember(Visibility = Visibility.Family, IsVirtual = true,
                          OverrideAction = MemberOverrideAction.Ignore)]
        public void OnPropertyChanged(string propertyName)
        {
// ReSharper disable RedundantThisQualifier
            if (this.PropertyChanged != null)
// ReSharper restore RedundantThisQualifier
            {
// ReSharper disable RedundantThisQualifier
                this.PropertyChanged(this.Instance,
// ReSharper restore RedundantThisQualifier
                                      new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Event introduced in the target type (unless it is already present);
        /// raised whenever a property has changed.
        /// </summary>
        [IntroduceMember(OverrideAction = MemberOverrideAction.Ignore)]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Method intercepting any call to a property setter.
        /// </summary>
        /// <param name="args">Aspect arguments.</param>
        [OnLocationSetValueAdvice,
         MulticastPointcut(Targets = MulticastTargets.Property,
             Attributes = MulticastAttributes.Instance)]
        public void OnPropertySet(LocationInterceptionArgs args)
        {
            // Don't go further if the new value is equal to the old one.
            // (Possibly use object.Equals here).
            if (args.Value == args.GetCurrentValue()) return;

            // Actually sets the value.
            args.ProceedSetValue();

            // Invoke method OnPropertyChanged (our, the base one, or the overridden one).
// ReSharper disable RedundantThisQualifier
            this.OnPropertyChangedMethod.Invoke(args.Location.Name);
// ReSharper restore RedundantThisQualifier

        }
    }
}
