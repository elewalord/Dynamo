﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using Dynamo.Utilities;
using Microsoft.Practices.Prism;
using Microsoft.Practices.Prism.Commands;
using Brush = System.Windows.Media.Brush;

namespace Dynamo.Connectors
{
    public class dynConnectorViewModel:dynViewModelBase
    {

        #region Properties

        private dynPortModel _activeStartPort;
        public dynPortModel ActiveStartPort { get { return _activeStartPort; } internal set { _activeStartPort = value; } }

        private dynConnectorModel _model;

        public DelegateCommand<object> ConnectCommand { get; set; }
        public DelegateCommand RedrawCommand { get; set; }
        public DelegateCommand HighlightCommand { get; set; }
        public DelegateCommand UnHighlightCommand { get; set; }

        public dynConnectorModel ConnectorModel
        {
            get { return _model; }
        }

        Brush _strokeBrush;
        public Brush StrokeBrush
        {
            get { return _strokeBrush; }
            set
            {
                _strokeBrush = value;
                RaisePropertyChanged("StrokeBrush");
            }
        }

        private bool _isConnecting = false;
        public bool IsConnecting
        {
            get { return _isConnecting; }
            set
            {
                _isConnecting = value;
                RaisePropertyChanged("IsConnecting");
            }
        }

        private bool _isHitTestVisible = false;
        public bool IsHitTestVisible
        {
            get { return _isHitTestVisible;  } 
            set { 
                _isHitTestVisible = value;
                RaisePropertyChanged("IsHitTestVisible");
            }
        }

        public double Left
        {
            get { return 0; }
        }

        public double Top
        {
            get { return 0; }
        }

        /// <summary>
        ///     The start point of the path pulled from the port's center
        /// </summary>
        public Point BezPoint0
        {
            get
            {
                if (_model == null)
                    return _activeStartPort.Center;
                else if (_model.Start != null)
                    return _model.Start.Center;
                else
                    return new Point();
            }
        }

        
        private Point _bezPoint1;
        public Point BezPoint1
        {
            get
            {
                return _bezPoint1;
            }
            set
            {
                _bezPoint1 = value;
                RaisePropertyChanged("BezPoint1");
            }
        }

        private Point _bezPoint2;
        public Point BezPoint2
        {
            get { return _bezPoint2; }
            set
            {
                _bezPoint2 = value;
                RaisePropertyChanged("BezPoint2");
            }
        }

        private Point _bezPoint3;
        public Point BezPoint3
        {
            get { return _bezPoint3; }
            set
            {
                _bezPoint3 = value;
                RaisePropertyChanged("BezPoint3");
            }
        }

        private double _dotTop;
        public double DotTop
        {
            get { return _dotTop; }
            set
            {
                _dotTop = value;
                RaisePropertyChanged("DotTop");
            }
        }

        private double _dotLeft;
        public double DotLeft
        {
            get { return _dotLeft; }
            set
            {
                _dotLeft = value;
                RaisePropertyChanged("DotLeft");
            }
        }

        private double _endDotSize = 6;
        public double EndDotSize
        {
            get { return _endDotSize; }
            set
            {
                _endDotSize = value;
                RaisePropertyChanged("EndDotSize");
            }
        }
        
        private const double HighlightThickness = 6;

        private double _strokeThickness = 2;
        public double StrokeThickness
        {
            get { return _strokeThickness; }
            set 
            {
                _strokeThickness = value;
                RaisePropertyChanged("StrokeThickness");
            }
        }

        /// <summary>
        /// Returns visible if the connectors is in the current space and the 
        /// model's current connector type is BEZIER
        /// </summary>
        public Visibility BezVisibility
        {
            get
            {
                if (dynSettings.Controller.DynamoViewModel.ConnectorType == ConnectorType.BEZIER)
                    return Visibility.Visible;
                return Visibility.Hidden;
            }
            set
            {
                RaisePropertyChanged("BezVisibility");
            }
        }

        /// <summary>
        /// Returns visible if the connectors is in the current space and the 
        /// model's current connector type is POLYLINE
        /// </summary>
        public Visibility PlineVisibility
        {
            get
            {
                if (dynSettings.Controller.DynamoViewModel.ConnectorType == ConnectorType.POLYLINE)
                    return Visibility.Visible;
                return Visibility.Hidden;
            }
            set
            {
                RaisePropertyChanged("PlineVisibility");
            }
        }


#endregion

        //construct a view and start drawing.
        public dynConnectorViewModel(dynPortModel port)
        {
            ConnectCommand = new DelegateCommand<object>(Connect, CanConnect);
            RedrawCommand = new DelegateCommand(Redraw, CanRedraw);
            HighlightCommand = new DelegateCommand(Highlight, CanHighlight);
            UnHighlightCommand = new DelegateCommand(Unhighlight, CanUnHighlight);

            var bc = new BrushConverter();
            _strokeBrush = (Brush)bc.ConvertFrom("#313131");

            IsConnecting = true;
            _activeStartPort = port;

            // makes sure that all of the positions on the curve path are
            // set
            this.Redraw(port.Center);

        }

        public dynConnectorViewModel(dynConnectorModel model)
        {
            ConnectCommand = new DelegateCommand<object>(Connect, CanConnect);
            RedrawCommand = new DelegateCommand(Redraw, CanRedraw);
            HighlightCommand = new DelegateCommand(Highlight, CanHighlight);
            UnHighlightCommand = new DelegateCommand(Unhighlight, CanUnHighlight);

            var bc = new BrushConverter();
            _strokeBrush = (Brush)bc.ConvertFrom("#313131");

            _model = model;
            
            _model.Start.PropertyChanged += Start_PropertyChanged;
            _model.End.PropertyChanged += End_PropertyChanged;
            _model.PropertyChanged += Model_PropertyChanged;

            dynSettings.Controller.DynamoViewModel.PropertyChanged += DynamoViewModel_PropertyChanged;
        }

        void ModelConnected(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Connect(object parameters)
        {
            //make the connector model
            var end = parameters as dynPortModel;

            _model = new dynConnectorModel(_activeStartPort.Owner, end.Owner, _activeStartPort.Index, end.Index, 0);
            _model.Connected += ModelConnected;

            _model.Start.PropertyChanged += Start_PropertyChanged;
            _model.End.PropertyChanged += End_PropertyChanged;
            dynSettings.Controller.DynamoViewModel.Model.PropertyChanged += Model_PropertyChanged;
            dynSettings.Controller.DynamoViewModel.PropertyChanged += DynamoViewModel_PropertyChanged;
            IsHitTestVisible = false;
        }

        void DynamoViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ConnectorType":
                    if (dynSettings.Controller.DynamoViewModel.ConnectorType == ConnectorType.BEZIER)
                    {
                        BezVisibility = Visibility.Visible;
                        PlineVisibility = Visibility.Hidden;
                    }
                    else
                    {
                        BezVisibility = Visibility.Hidden;
                        PlineVisibility = Visibility.Visible;
                    }
                break;
            }
        }

        void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CurrentSpace":
                    RaisePropertyChanged("BezVisibility");
                    RaisePropertyChanged("PlineVisibility");
                    break;
            }
        }

        void End_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Center")
            {
                Redraw();
            }   
        }

        void Start_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Center")
            {
                Redraw();
                RaisePropertyChanged("BezPoint0");
            }  
        }

        private bool CanConnect(object parameters)
        {
            if ((parameters as dynPortModel) == null)
                return false;

            return true;
        }

        /// <summary>
        ///     Recalculate the path points using the internal model.
        /// </summary>
        public void Redraw()
        {
            if (this.ConnectorModel.End != null)
                this.Redraw(this.ConnectorModel.End.Center);
        }

        /// <summary>
        ///     Recalculate the connector's points given the end point
        /// </summary>
        /// <param name="p2">The position of the end point</param>
        public void Redraw(Point p2 )
        {
            BezPoint3 = p2;

            var bezOffset = 0.0;
            double distance = BezPoint3.X - BezPoint0.X;
            if ( this.BezVisibility == Visibility.Visible )
            {
                bezOffset = .3 * distance;
            }
            else
            {
                bezOffset = distance / 2;
            }

            BezPoint1 = new Point(BezPoint0.X + bezOffset, BezPoint0.Y);
            BezPoint2 = new Point(p2.X - bezOffset, p2.Y);

            DotTop = BezPoint3.Y - EndDotSize / 2;
            DotLeft = BezPoint3.X - EndDotSize / 2;

        }

        private bool CanRedraw()
        {
            return true;
        }

        private void Highlight()
        {
            StrokeThickness = HighlightThickness;
        }

        private bool CanHighlight()
        {
            return true;
        }

        private void Unhighlight()
        {
            StrokeThickness = _strokeThickness;
        }

        private bool CanUnHighlight()
        {
            return true;
        }
    }
}