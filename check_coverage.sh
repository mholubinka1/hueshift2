#!/bin/bash

# TODO: deal with no previous pipeline / null coverage value

TARGET_BRANCH="master"
JOB_NAME="build-common"

TARGET_PIPELINE_ID=`curl -s "${CI_API_V4_URL}/projects/${CI_PROJECT_ID}/pipelines?ref=${TARGET_BRANCH}&status=success&private_token=${RUNNER_ACCESS_TOKEN}" | jq "[.[] ] | .[0].id"`
TARGET_COVERAGE=`curl -s "${CI_API_V4_URL}/projects/${CI_PROJECT_ID}/pipelines/${TARGET_PIPELINE_ID}/jobs?private_token=${RUNNER_ACCESS_TOKEN}" | jq --arg JOB_NAME "$JOB_NAME" '.[] | select(.name==$JOB_NAME) | .coverage'`
echo "Target coverage value = " $TARGET_COVERAGE

CURRENT_COVERAGE=`curl -s "${CI_API_V4_URL}/projects/${CI_PROJECT_ID}/pipelines/${CI_PIPELINE_ID}/jobs?private_token=${RUNNER_ACCESS_TOKEN}" | jq --arg JOB_NAME "$JOB_NAME" '.[] | select(.name==$JOB_NAME) | .coverage'`
echo "Pipeline " ${CI_PIPELINE_ID} " coverage value = " $CURRENT_COVERAGE


if [ "$CURRENT_COVERAGE" -ge "$TARGET_COVERAGE"]
then
  echo "Build Succeeded."
  exit 0
else
  echo "Coverage decreased from ${TARGET_COVERAGE} to ${CURRENT_COVERAGE}. Build Failed."
  exit 1
fi