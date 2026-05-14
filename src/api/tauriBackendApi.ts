import { invoke } from "@tauri-apps/api/core";

export function getBackendBaseUrl(): Promise<string> {
  return invoke<string>("get_backend_base_url");
}
