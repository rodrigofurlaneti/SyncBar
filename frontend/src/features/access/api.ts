import { api } from "../../lib/apiClient";
import type { FeatureResponse, MyFeaturesResponse } from "../../lib/types";

export const getMyFeatures = (): Promise<MyFeaturesResponse> =>
  api<MyFeaturesResponse>("/api/access/my-features");

export const getFeatures = (): Promise<FeatureResponse[]> =>
  api<FeatureResponse[]>("/api/access/features");

export const getJobTitleFeatures = (jobTitleId: number): Promise<number[]> =>
  api<number[]>(`/api/access/jobtitles/${jobTitleId}/features`);

export const setJobTitleFeatures = (jobTitleId: number, featureIds: number[]): Promise<void> =>
  api<void>(`/api/access/jobtitles/${jobTitleId}/features`, {
    method: "PUT",
    body: JSON.stringify({ featureIds }),
  });

export const getUserFeatures = (appUserId: number): Promise<number[]> =>
  api<number[]>(`/api/access/users/${appUserId}/features`);

export const setUserFeatures = (appUserId: number, featureIds: number[]): Promise<void> =>
  api<void>(`/api/access/users/${appUserId}/features`, {
    method: "PUT",
    body: JSON.stringify({ featureIds }),
  });
