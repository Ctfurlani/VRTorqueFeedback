# VRTorqueFeedback
Software and Hardware to emulate haptic feedback of weighted objects in 
VR.

## Principle Of Operation
The idea is that the pointer (sometimes refered to as "feedback" in the 
code) moves/rotates in order to shift the center of mass of the
controller. This creates a torque that mimics the direction and
intensity of the one that would be exerted by the virtual object being
held.

<p align="center"> <img src="Doc/Pictures/torque.jpg" align="center" height=200> </p> 

## Calculating Pointer Position
If we imagine the feedback stem/pointer as a vector in R3, we can define
references and directions of motion to use in our program. In "home"
position, the vector points up, aligned with the Y axis. From this
position, it can rotate alpha degrees around the X axis, and Gamma
degrees around the Z axis, reaching any position in a hemisphere of
positive Y values.

We gain ease of understanding and implementation by decomposing the
ideal torque in two components Tx and Tz (projections over the X and Z
axis). To emulate Tx and Tz on the user's hand, the feedback must rotate
alpha degrees around X (for Tx), and gamma degrees around Z (for Tz).
The combination of these two rotations yields the sum of Tx and Tz,
which is the real torque T.

<p align="center"> <img src="Doc/Pictures/servos_alpha_gamma.jpg" height=200> </p> 

Ideal torque T will be achieved if and only if the pointer is rotated by
alpha and gamma, so the problem lies in calculating these two angles.
Luckily, the decomposition chosen facilitates this task by letting us
analyse torque decompositions with coplanar vectors *Rx*,*Wx* and *Rz*,
*Wx* - projections of R and W on X and Z.

We obtain ideal torque vector directly with

<!-- **T** = **R** x **W** -->
<p align="center"> <img src="Doc/Pictures/t_r_cross_w.png" height="20"> </p> 

Then, we split it in two components, projections on X and Z:

<!-- TprojX = **T** . **X** = |**Tx**| * signX, where signX is 1 or -1 -->
<p align="center"> <img src="Doc/Pictures/t_proj_x.png" height="35"> </p> 

<!-- TprojZ = **T** . **Z** = |**Tz**| * signZ, where signX is 1 or -1 -->
<p align="center"> <img src="Doc/Pictures/t_proj_z.png" height="35"> </p> 

Where signx and signz follow the rule
<p align="center"> <img src="Doc/Pictures/sign.png" height="75"> </p> 

We obtain the angles alpha' ang gamma' from the cross product magnitude
formula and considering that dot product is a linear operation (used to
calculate **Tx** and **Tz**).

<!-- |**Tx**| = |**Rx**| * |**Wx**| * sin(alpha') , where a E [0, 90] -->
<p align="center"> <img src="Doc/Pictures/tx_sin_alpha.png" height="30"> </p> 

<!-- |**Tz**| = |**Rz**| * |**Wz**| * sin (gamma') , where g E [0, 90] -->
<p align="center"> <img src="Doc/Pictures/tz_sin_gamma.png" height="30"> </p> 

<p align="center"> <img src="Doc/Pictures/alpha_prime_gamma_prime_in.png" height="30"> </p> 

We then solve for alpha' and gamma', while substituting |**Wx**| =
|**Wz**| = |**W**| for m * g (to account for real feedback weight);
|**Rx**| for rx * l; and |**Rz**| for rz * l (to account for real
feedback length, but keeping proportions between **Rx** and **Rz**).

<!-- alpha' = arcsin((|**Tx**|) / |**Rx**| * l * m * g) -->
<p align="center"> <img src="Doc/Pictures/alpha_prime.png" height="65"> </p> 

<!-- gamma' = arcsin((|**Tz**|) / |**Rz**| * l * m * g) -->
<p align="center"> <img src="Doc/Pictures/gamma_prime.png" height="65"> </p> 

<p align="center"> <img src="Doc/Pictures/rx_rz.png" height="75"> </p> 

We could simplify l * m * g to a single constant c, but it's useful to
have it separated into the two tangible variables l and m because of the
attribution of weights and distances in metric units in the virtual
scenario (in Unity they are actually dimensionless, but if we assume
meters for distance, we must also assume kilograms for mass).

Finally, we obtain alpha and gamma from alpha' and gamma', attributing
it the correct signs to achieve full hemispherical reach.

<!-- alpha = alpha' * signX, where alpha E [-90, 90] -->
<p align="center"> <img src="Doc/Pictures/alpha_signed.png" height="35"> </p> 

<!-- gamma = gamma' * signZ, where gamma E [-90, 90] -->
<p align="center"> <img src="Doc/Pictures/gamma_signed.png" height="35"> </p> 

<p align="center"> <img src="Doc/Pictures/alpha_gamma_in.png" height="30"> </p> 

### "Neutralizing" Center of Mass
When the user is not holding any object, the program can rotate the
feeeback pointer so that it shifts the controller's center of mass to a
position where (ideally) no torque would be exerted over the player's
hand. The heuristic to achieve this effect is to rotate the pointer
according to the rotation of the controller, in the opposite direction
of rotation around X and Z axes. As an example, if the user tilts the
controller by 30 degrees around the X axis and 45 degrees around the Z
axis, the pointer will rotate by -30 and -45 degrees (maybe multiplied
by a proportionality constant) around X and Z to compensate.

<p align="center"> <img src="Doc/Pictures/neutral_center_of_mass.jpg" height=200> </p> 

## Obtaining Angles for Servos
The angles alpha and gamma are not the ones to be used by the servo
motors. This is because the two DoFs of the motors are not the same as
the ones used in our previous calculations (rotations around X and Z
axes). The servos combine theta (angles from Z in the XZ plane) and phi
(angles from Y axis) rotations in spherical coordinates, with fixed
radius. This angles must be derived unambiguously from the position of
the feedback pointer, to provide the mirroring from the virtual pointer
to the real one.

<p align="center">
    <img src="Doc/Pictures/spherical_coords_unity_axes.jpg" height=200> 
    <img src="Doc/Pictures/servos_theta_phi.jpg" height=200>
</p> 

## Moving the Servos
Movement of the servo motors is coordinated by the ARDUnity plugin. With
its "wire editor", we create a serial communication bridge between Unity
and an arduino. The serial communication transmits data to coordinate
two servos (represented by two "Generic Servo" components), which get
the desired angle of rotation from a direct mapping of the rotation of a
virtual object (with the components/scripts "Rotation Axis Reactor").
With this setup, to achieve rotations by X degrees in a servo, we simply
rotate a linked virtual object by X degrees in a set direction. In our
scene, these objects are the "Mirror Servo Theta" and "Mirror Servo
Phi".

<p align="center">
    <img src="Doc/Pictures/ardunity_wires.jpg" height=200>
    <img src="Doc/Pictures/mirror_servos_with_theta_phi.jpg" height=200>
</p> 