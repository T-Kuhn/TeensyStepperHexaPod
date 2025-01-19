// - [X] cleanup code
// - [X] Add a thing that let's us quickly switch between witch solution is used
// - [X] make our current arm work no matter how it is rotated (calculations need to be in local space)
// - [X] make our current arm work no matter where it is placed
// - [X] would be nice if we had gizmo lines for all joints maybe?
// - [X] also make upper joint move (keep this one parallel to floor)
// - [X] automatically instanciate arms at sensible positions
//     - [X] instantiate end effector platform on play
//     - [X] instantiate 6 arms on play (setup target positions inside end effector platform)
//     - [X] parameters for easy experimentation:
//           - distance from center for motors pairs at midpoint
//           - distance apart of motor pair
//           - distance from center for target pairs below endeffector at midpoint
//           - distance apart of target pair
// - [ ] add some sort of animation for end effector platform
//     - [X] moving up and down
//     - [X] moving left and right
//     - [X] moving forward and backward
//     - [X] circling (position)
//     - [X] circling + up/down (position)
//     - [X] tilting around the X-Axis
//     - [ ] tilting around the Z-Axis
//     - [ ] circling (tilt)
//     - [ ] moving up and down at left/right/front/back position
// - [ ] Fix joints-can't-handle-tilt-problem (need more degrees of freedom for top most joint)
//     - it seems like turning the top U-joint by 90/45 degrees and adding a single rotating joint along the direction of the
//       connection should solve our issue.
// - [ ] Fix last joint not rotating together with tilted platform problem
// - [ ] center gizmo would be nice, so we can tell how far away from center the hexaplate is
// - [ ] change colors of arm parts which are colliding so that we can get a better feeling of how the arm design has to be improved
// - [ ] iterate on arm design based on what we'll see in the simulated machine
// - [ ] maybe switch project to the High Definition Render Pipeline (HDRP) with demo scene as base for better visual look
// - [ ] Need a name for the thing. Some ideas:
//   - u-joint. The UJ-Table
//   - hexapod. The Hexa Bot.

// Thing to check:
// - Wouldn't it also work if the 6 arms were placed one at a time at 60deg intervals instead of pairwise in 120deg intervals?

