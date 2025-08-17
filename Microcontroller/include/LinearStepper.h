/*
  LinearStepper library
  Author: T-Kuhn.
  Sapporo, November, 2024. Released into the public domain.
  */

#ifndef LinearStepper_h
#define LinearStepper_h
#include "Constants.h"
#include "Arduino.h"

class LinearStepper
{
public:
  LinearStepper(uint8_t pinStep, uint8_t pinDir, uint8_t stepperID);
  void update(float t);
  void setGoalPos(int32_t goalPos);
  void setStepsToTakeToZero();
  int32_t currentPos;
  int8_t id;

private:
  uint8_t pulseFromAmplitude(float stepsToTake, float t);
  int32_t _goalPosition;
  int32_t _currentStepsToTake;
  uint8_t _pinStep;
  uint8_t _pinDir;
  uint8_t _lastPulse;
  bool _isMovingCW;
};

#endif