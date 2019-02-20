# VRTorqueFeedback
Software and Hardware to emulate haptic feedback of weighted objects in VR.

## Principle Of Operation
The idea is that the pointer (sometimes refered to as "feedback" in the code) moves/rotates in order to shift the center of mass of the controller. This creates a torque that mimics the direction and intensity of the one that would be exerted by the virtual object being held.

<img src="Doc/Pictures/torque.jpg" height=200px>

## Calculating Pointer Position
If we imagine the feedback stem/pointer as a vector in R3, we can define references and directions of motion to use in our program. In "home" position, the vector points up, alligned with the Y axis. From this position, it can rotate alpha degrees around the X axis, and Gamma degrees around the Z axis, reaching any position in a hemisphere of positive Y values.

We gain ease of understanding and implementation by decomposing the ideal torque in two components Tx and Tz (projections over the X and Z axis). To emulate Tx and Tz on the user's hand, the feedback must rotate alpha degrees around X (for Tx), and gamma degrees around Z (for Tz). The combination of these two rotations yields the sum of Tx and Tz, which is the real torque T. 

Ideal torque T will be achieved if and only if the pointer is rotated by alpha and gamma, so the problem lies in calculating these two angles. Luckily, the decomposition chosen facilitates this task by letting us calculate torques with coplanar vectors *length* and *weight* by *t* = *length* * *weight* * *sin (beta)* , where *beta* is the angle between the vectors. Beta is a function of alpha or gamma identifiable by analysis of the trigonometric relations involved in the problem.

**TODO write and explain final equation of angles**

**Picture of decompositions and angles**

<img src="Doc/servos_alpha_gamma.jpg" height=200px>

### "Neutralizing" Center of Mass
When the user is not holding any object, the program can rotate the feedback pointer so that it shifts the controller's center of mass to a position where (ideally) no torque would be exerted over the player's hand. The heuristic to achieve this effect is to rotate the pointer according to the rotation of the controller, in the opposite direction of rotation around X and Z axes. As an example, if the user tilts the controller by 30 degrees around the X axis and 45 degrees around the Z axis, the pointer will rotate by -30 and -45 degrees (maybe multiplied by a proportionality constant) around X and Z to compensate.

<img src="Doc/neutral_center_of_mass.jpg" height=200px>

## Obtaining Angles for Servos
The angles alpha and gamma are not the ones to be used by the servo motors. This is because the two DoFs of the motors are not the same as the ones used in our previous calculations (rotations around X and Z axes). The servos combine theta (angles from Z in the XZ plane) and phi (angles from Y axis) rotations in spherical coordinates, with fixed radius. This angles must be derived unambiguously from the position of the feedback pointer, to provide the mirroring from the virtual pointer to the real one.

<img src="Doc/spherical_coords_unity_axes.jpg" height=200px>
<img src="Doc/servos_theta_phi.jpg" height=200px>

## Moving the Servos
Movement of the servo motors is coordinated by the ARDUnity plugin. With its "wire editor", we create a serial communication bridge between Unity and an arduino. The serial communication transmits data to coordinate two servos (represented by two "Generic Servo" components), which get the desired angle of rotation from a direct mapping of the rotation of a virtual object (with the components/scripts "Rotation Axis Reactor"). With this setup, to achieve rotations by X degrees in a servo, we simply rotate a linked virtual object by X degrees in a set direction. In our scene, these objects are the "Mirror Servo Theta" and "Mirror Servo Phi".

<img src="Doc/ardunity_wires.jpg" height=200px>
<img src="Doc/mirror_servos_with_theta_phi.jpg" height=200px>