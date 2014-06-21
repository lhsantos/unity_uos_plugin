package org.unbiquitous.unity.androidnetwork;

import java.io.IOException;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.net.SocketException;

public class TcpListener {
	private ServerSocket socket;

	public TcpListener(String host, int port) throws IOException {
		socket = new ServerSocket(port, 0, InetAddress.getByName(host));
	}

	public boolean getReuseAddress() throws SocketException {
		return socket.getReuseAddress();
	}

	public void setReuseAddress(boolean value) throws SocketException {
		socket.setReuseAddress(value);
	}

	public TcpClient acceptTcpClient() throws IOException {
		return new TcpClient(socket.accept());
	}

	public boolean isPending() {
		return true;
	}

	public void start() {
	}

	public void stop() {
	}
}
