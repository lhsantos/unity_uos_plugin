package org.unbiquitous.unity.androidnetwork;

import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.io.IOException;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.net.SocketException;

public class TcpClient {
	Socket tcpSocket;

	public TcpClient(Socket socket)
	{
		tcpSocket = socket;
	}
	
	public TcpClient() {
		tcpSocket = new Socket();
	}

	public String getHost() {
		return tcpSocket.getInetAddress().getHostAddress();
	}

	public int getPort() {
		return tcpSocket.getPort();
	}

	public boolean isConnected() {
		return tcpSocket.isConnected();
	}

	public int getReceiveTimout() throws SocketException {
		return tcpSocket.getSoTimeout();
	}

	public void setReceiveTimout(int value) throws SocketException {
		tcpSocket.setSoTimeout(value);
	}

	public int getSendTimeout() {
		return 0;
	}

	public void setSendTimout(int value) {
	}

	public boolean getReuseAddress() throws SocketException {
		return tcpSocket.getReuseAddress();
	}

	public void setReuseAddress(boolean value) throws SocketException {
		tcpSocket.setReuseAddress(value);
	}

	public void connect(String host, int port) throws IOException {
		tcpSocket.connect(new InetSocketAddress(host, port));
	}

	public DataInputStream createInputStream() throws IOException {
		return new DataInputStream(tcpSocket.getInputStream());
	}

	public DataOutputStream createOutputStream() throws IOException {
		return new DataOutputStream(tcpSocket.getOutputStream());
	}

	public void close() throws IOException {
		tcpSocket.close();
	}
}
