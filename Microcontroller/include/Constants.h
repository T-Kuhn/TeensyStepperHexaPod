/*
  Constants
  Author: T-Kuhn.
  Sapporo, November, 2024. Released into the public domain.
  */

#ifndef Constants_h
#define Constants_h
#include "Arduino.h"

#define STEPPER1_DIR_PIN 1
#define STEPPER1_STEP_PIN 2
#define STEPPER2_DIR_PIN 5
#define STEPPER2_STEP_PIN 6
#define STEPPER3_DIR_PIN 3
#define STEPPER3_STEP_PIN 4
#define STEPPER4_DIR_PIN 7
#define STEPPER4_STEP_PIN 8
#define STEPPER5_DIR_PIN 14
#define STEPPER5_STEP_PIN 15
#define STEPPER6_DIR_PIN 16
#define STEPPER6_STEP_PIN 17

#define NAN_ALERT_LED 25
#define RESET_BUTTON_PIN 12
#define EXECUTING_ISR_CODE 13

  // gear: 26.85:1
  // without gear:
  // 200 steps / rev (full step mode)

  // with gear:
  // 5370 steps / rev (full step mode)
  // 10740 steps / rev (1/2 step mode)
  // 21480 steps / rev (1/4 step mode)
  // 42960 steps / rev (1/8 step mode)
  // 85920 steps / rev (1/16 step mode)
  //
  // we want to figure out what setting will allow us to do 1 full rev the fastest.

// #define PULSES_TO_MOVE 4000
// 20 * 1600 / (3.1415 * 2) = 5093.10838771 steps in 0.2 seconds
// --> 25k steps per second

#define PULSES_PER_REV 64000 // MicroStep setting: 6400, gear 10:1
#define MOVE_DURATION 1.0f
#define PAUSE_DURATION 0.2f

// NOTE: Our pulse generation timer runs at 4μs and thus our max pulses per second is 125kHz.
#define FREQUENCY_MULTIPLIER 0.000004f
#define TIMER_US 4

// NOTE: LinearStepper and MoveBatch ids must be lower then MAX_NUM_OF_STEPPERS
#define MAX_NUM_OF_STEPPERS 10
#define MAX_NUM_OF_MOVEBATCHES 1000

// Max input size for the list of incoming instructions
// NOTE: an average move batch is made of around 100 chars. If one stringed instructions is 
//       made out of 50 small move batches what will be around 5000 chars.
//       So below INPUT_SIZE allows for around 10 such stringed instructions to be buffered.
#define INPUT_SIZE 51200

#endif