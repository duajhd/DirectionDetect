   M S B A M L   `   `   `  ����M  IDirectionDetection, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullE'clr-namespace:DirectionDetection.SharedDirectionDetection.Shared  5   	   7 clr-namespace:DirectionDetectionDirectionDetection  5   	   #    DirectionDetection.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   >
converters'clr-namespace:DirectionDetection.Shared     5   	   2local clr-namespace:DirectionDetection     5   	   -   5          � Title$  
MainWindow��5	   	   $	��450��6   $	��800��6)    � 	Resources 5
      4   .DirectionDetection.Shared.BoolToBrushConverter   BoolToBrushConverter �� 
TargetType  IndicatorLightStyle �� Property �� Value��%�   &	        &	        5   
   6I   �� 5   
   " A�6   .�5      ��  � ��$ 30��6&   �� 5       � ��$ 30��6'   �� 5       � ��$ 2��60   �� 5       � ��
 �  ��6%   5      5      .��5      �  � RowDefinitions 5   
   �� 5      $��35 �6   6*   �� 5      6   .+�5   
   � .+�5      o� $	��Top=�6   .@�5       m� -   5         m�Name$ parametersetting��5    )   $��参数设置��6   5!      m� 5"      -   5        $ calibration��5"   '   $��标定��6   6>   5$      5%      � 5&   
   $��1��6    � ColumnDefinitions 5'      �� 5(      6%   �� 5)      $	��200 �6#   61   .+�5+      � $��0��6    5,      �� 5-      6&   �� 5.      $��30 �6$   62   .+�50      �� -   5         ��Name$ 	DotCanvas��50   '   $��0��6   52      �� 53      $��1��6   	 �� Orientation$	 
Horizontal=�6*   .+�54      �� $��Center=�6    $��16(�6<   $
��Left=�6J   PLC连接：6m   A� 55      -   5        
 A�Name$
 PLCState��55      #��[ 60   {�56      �� *IsPLCConnected+�� 57      $��16(�6    $��Center=�6.   $
��Left=�6J   相机连接：6l   A� 58      -   5        $
 CameraState��58      #��[ 63   {�59      �� *IsInsideCamerConnected+5?      5A      A� 5B       A� Rows$ 5��6   $��1��6#   .+�5C      �� $	 
Horizontal=�6   .+�5D      �� $��Center=�6    $��40��6;   	行坐标6K   �� 5E      -   5         ��Name$ rowss��5E       $��Center=�6/   $	��100��6K   6Y   5F      �� 5G      $	 
Horizontal=�6   .+�5H      �� $��Center=�6    $��40��6;   	列坐标6K   �� 5I      -   5        $ colss��5I       $��Center=�6/   $	��100��6K   6Y   5J      �� 5L      -	   5         ��Name$ 
InitialBtn��5L   <   $	��100��6   .��6P   	初始化6U   �� 5N      -
   5        $ StartingBtn��5N   <   $	��100��6    � Cursor$
 Hand��6Q   ${�False��6_   .��6q   启动5P      5Q      5S      5U      5V      