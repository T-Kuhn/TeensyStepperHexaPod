﻿/*
  SineStepperController library
  Author: T-Kuhn.
  Sapporo, November, 2024. Released into the public domain.
*/

#ifndef SineStepperController_h
#define SineStepperController_h

#include "Constants.h"
#include "Arduino.h"
#include "SineStepper.h"
#include "MoveBatch.h"

class SineStepperController
{
public:
  SineStepperController(bool repeat);
  bool update();
  void attach(SineStepper* sStepper);
  void resetMoveBatchExecution();
  MoveBatch moveBatches[MAX_NUM_OF_MOVEBATCHES];

private:
  void setFrequencyFrom(float moveDuration);
  bool _isExecutingBatch;
  bool _endlessRepeat;
  uint8_t _numOfAttachedSteppers;
  uint32_t _counter = 0;
  uint32_t _currentMoveBatchIndex = 0;
  SineStepper* _sineSteppers[MAX_NUM_OF_STEPPERS];
  float _frequency;
};

#endif