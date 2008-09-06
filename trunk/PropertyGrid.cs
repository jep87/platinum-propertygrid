﻿#region LGPL.
// -----------------------------------------------------------------------------
// This source file is part of Platinum.PropertyGrid
// For the latest info, see http://code.google.com/p/platinum-propertygrid/
//
// Copyright (c) 2008 Michael Woerister
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
// or go to http://www.gnu.org/copyleft/lesser.txt.
// -----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace Platinum
{
    public partial class PropertyGrid : UserControl
    {
        #region SectionCollection struct
        struct SectionCollection : IPropertyGridSectionCollection
        {
            PropertyGrid _owner;

            public SectionCollection( PropertyGrid owner )
            {
                _owner = owner;
            }

            public IPropertyGridSection this[string name]
            {
                get 
                { 
                    return _owner._sections.SingleOrDefault( x => x.SectionName == name ); 
                }
            }

            public IPropertyGridSection this[int index]
            {
                get 
                { 
                    return _owner._sections[index];
                }
            }

            public IPropertyGridSection Add( String name )
            {
                return _owner._addSection( name );
            }

            public void Remove( String name )
            {
                _owner._removeSection( name );
            }

            public void Clear()
            {
                _owner._clearSections();
            }

            public int Count
            {
                get { return _owner._sections.Count; }
            }
            
            public IEnumerator<IPropertyGridSection> GetEnumerator()
            {
                return _owner._sections.Cast<IPropertyGridSection>().GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _owner._sections.GetEnumerator();
            }
        }
        #endregion

        #region Variables
        List<PropertyGridSection> _sections = new List<PropertyGridSection>();
        Color _sectionBackColor = Color.FromArgb( 233, 236, 250 );
        bool _updatingSelection;
        #endregion

        #region Events
        public event EventHandler<ColorChangedEventArgs> SectionBackColorChanged;
        #endregion

        #region Properties
        [Browsable( true )]
        [Description("The background color of all contained sections.")]
        [Category("Appearance")]
        public Color SectionBackColor
        {
            get { return _sectionBackColor; }
            set 
            {
                Color oldColor = _sectionBackColor;
                _sectionBackColor = value;

                if ( SectionBackColorChanged != null )
                {
                    ColorChangedEventArgs e = new ColorChangedEventArgs( oldColor, value );
                    SectionBackColorChanged( this, e );
                }
            }
        }

        [Browsable( false )]
        public IPropertyGridSectionCollection Items
        {
            get { return new SectionCollection( this ); }
        }
        #endregion

        #region Constructor
        public PropertyGrid()
        {
            InitializeComponent();
        }
        #endregion
        
        #region EventHandler
        void _section_SizeChanged( object sender, EventArgs e )
        {
            _updateSectionPositions();
        }

        void _section_SelectedItemChanged( object sender, EventArgs e )
        {
            if ( !_updatingSelection )
            {
                _updatingSelection = true;

                _helpTextTitleLabel.Text = "";
                _helpTextLabel.Text = "";

                PropertyGridSection selectedSection = (PropertyGridSection) sender;

                foreach ( PropertyGridSection section in _sections )
                {
                    if ( selectedSection != section )
                    {
                        section.ResetSelectedItem();
                    }
                    else
                    {
                        PropertyGridItem selectedItem = section.SelectedItem;

                        if ( selectedItem != null && selectedItem.Description != null )
                        {
                            _helpTextTitleLabel.Text = selectedItem.Name;
                            _helpTextLabel.Text = selectedItem.Description;
                        }
                    }
                }

                _updatingSelection = false;
            }
        }

        void _section_SplitterMoving( object sender, SplitterCancelEventArgs e )
        {
            foreach ( PropertyGridSection section in _sections )
            {
                section.SplitterDistance = e.SplitX;
            }
        }

        void _splitContainer_Panel1_SizeChanged( object sender, EventArgs e )
        {
            _updateSectionPositions();
        }

        void _sectionPanelScrollBar_Scroll( object sender, ScrollEventArgs e )
        {
            _sectionPanel.Top = -e.NewValue;
        }
        #endregion

        #region Private Methods
        PropertyGridSection _addSection( String sectionName )
        {
            Debug.Assert( !_sections.Any( x => x.SectionName == sectionName ) );

            PropertyGridSection section = new PropertyGridSection();
            section.SectionName = sectionName;

            int y = _sections.Sum( sec => sec.Height );

            _sections.Add( section );

            _sectionPanel.Controls.Add( section );

            section.Top = y;
            section.Left = 0;
            section.Width = _sectionPanel.Width;
            section.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            section.SizeChanged += new EventHandler( _section_SizeChanged );
            section.SplitterMoving += new SplitterCancelEventHandler( _section_SplitterMoving );
            section.SelectedItemChanged += new EventHandler( _section_SelectedItemChanged ); 
            section.Owner = this;
            
            _updateSectionPositions();

            return section;
        }

        void _removeSection( String name )
        {
            PropertyGridSection section =
                _sections.SingleOrDefault( x => x.SectionName == name );

            Debug.Assert( _sections.Contains( section ) );

            section.SizeChanged -= _section_SizeChanged;
            _sections.Remove( section );

            section.Dispose();

            _updateSectionPositions();
        }

        void _clearSections()
        {
            while ( _sections.Any() )
            {
                _removeSection( _sections.Last().SectionName );
            }
        }

        void _updateSectionPositions()
        {
            int height = _sections.Sum( section => section.Height );

            if ( _sectionPanel.Height != height )
            {
                _sectionPanel.Height = height;
            }
            
            if ( _sectionPanel.Height > _splitContainer.Panel1.Height )
            {
                if ( !_sectionPanelScrollBar.Visible )
                {
                    _sectionPanelScrollBar.Visible = true;
                    _sectionPanelScrollBar.Enabled = true;
                }

                if ( _sectionPanel.Width != _splitContainer.Panel1.Width - _sectionPanelScrollBar.Width )
                {
                    _sectionPanel.Width = _splitContainer.Panel1.Width - _sectionPanelScrollBar.Width;
                }

                int max = _sectionPanel.Height;
                int largeChange = _splitContainer.Panel1.ClientSize.Height;

                _sectionPanelScrollBar.Minimum = 0;
                _sectionPanelScrollBar.Maximum = max;
                _sectionPanelScrollBar.LargeChange = largeChange;
                _sectionPanelScrollBar.SmallChange = 1;

                if ( _sectionPanelScrollBar.Value > max - largeChange + 1 )
                {
                    _sectionPanelScrollBar.Value = max - largeChange + 1;
                }

                if ( _sectionPanel.Top != -_sectionPanelScrollBar.Value )
                {
                    _sectionPanel.Top = -_sectionPanelScrollBar.Value;
                }
            }
            else
            {
                if ( _sectionPanelScrollBar.Visible )
                {
                    _sectionPanelScrollBar.Visible = false;
                    _sectionPanelScrollBar.Enabled = false;
                }

                if ( _sectionPanel.Width != _splitContainer.Panel1.Width )
                {
                    _sectionPanel.Width = _splitContainer.Panel1.Width;
                }

                if ( _sectionPanel.Top != 0 )
                {
                    _sectionPanel.Top = 0;
                }
            }

            int y = 0;

            foreach ( PropertyGridSection section in _sections )
            {
                if ( section.Top != y )
                {
                    section.Top = y;
                }

                if ( section.Width != _sectionPanel.Width )
                {
                    section.Width = _sectionPanel.Width;
                }

                y += section.Height;
            }
        }
        #endregion
    }

    public class ColorChangedEventArgs : EventArgs
    {
        public readonly Color OldColor;
        public readonly Color NewColor;

        public ColorChangedEventArgs( Color oldColor, Color newColor )
        {
            OldColor = oldColor;
            NewColor = newColor;
        }
    }
}